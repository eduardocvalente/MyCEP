using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MyCEP.API;
using MyCEP.API.Core.API.Middleware;
using MyCEP.API.Core.Shared.Infrastructure.Database.Postgres;
using MyCEP.API.Core.Shared.Infrastructure.IoC;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var configuration = LambdaConfiguration.GetConfiguration();
builder.Services.RegisterServicesToLambda(configuration, builder.Environment.IsDevelopment());

// Console em JSON estruturado fora de desenvolvimento: o CloudWatch (e
// qualquer agregador) indexa campos em vez de fazer grep em texto livre.
if (!builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddJsonConsole(options =>
    {
        options.TimestampFormat = "O";
        options.UseUtcTimestamp = true;
    });
}

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<MyCepDbContext>(name: "postgres", tags: ["ready"]);

// API pública somente leitura: qualquer origem pode consultar via browser,
// mas apenas com GET.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .WithMethods("GET")
              .AllowAnyHeader());
});

// Cada consulta que não está em cache vira uma chamada ao ViaCEP e um INSERT.
// Sem limite, um loop de CEPs aleatórios enche o banco e derruba a cota no
// ViaCEP. 60 req/min por IP é folgado para uso legítimo.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 60,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
});

// Em produção o Caddy termina a conexão e repassa a requisição por HTTP.
// Sem isso o ASP.NET enxerga todo tráfego como http e com o IP do proxy.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    // O proxy é o único que alcança a porta 8080 (rede interna do Docker,
    // nunca publicada no host), então não há de quem se defender aqui.
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();
app.UseMiddleware<ApiMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    await next();
});

// Depois do ForwardedHeaders, para o limite valer por IP real do cliente
// e não pelo IP do Caddy.
app.UseRateLimiter();

app.UseCors();

// Swagger fica ligado também em produção, ao contrário do padrão do template.
// Esta é uma API pública, sem autenticação e somente leitura: não há superfície
// a esconder, e numa demo de portfólio a documentação navegável é o que o
// visitante vê primeiro. Numa API autenticada ou com dados sensíveis, mantenha
// restrito ao ambiente de desenvolvimento.
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "MyCEP.API v1");
    options.RoutePrefix = string.Empty; // serve na raiz do domínio
    options.DocumentTitle = "MyCEP.API";
});

if (app.Environment.IsDevelopment())
{
    // Atrás do proxy quem faz o redirect é o Caddy; ligar aqui causaria
    // loop de redirecionamento.
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();

// /health/live responde sem tocar o banco (usado pelo Docker healthcheck).
// /health valida a dependência do Postgres.
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/health");

// Atenção: tem que ser "configuration" (a que o LambdaConfiguration monta a
// partir das variáveis de ambiente), e não "app.Configuration" — são dois
// objetos distintos, e a chave só existe no primeiro.
if (configuration.GetValue<bool>("Database:MigrateOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Aplicando migrations pendentes...");
    await scope.ServiceProvider.GetRequiredService<MyCepDbContext>().Database.MigrateAsync();
    logger.LogInformation("Migrations aplicadas.");
}

app.Run();

// Adicionar esta classe para o WebApplicationFactory
public partial class Program { }
