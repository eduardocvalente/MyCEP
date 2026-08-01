using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using MyCEP.API.Core.Shared.Infrastructure.Database.Postgres;
using MyCEP.API.Core.Shared.Infrastructure.Gateways;
using MyCEP.API.Core.Shared.Infrastructure.IoC;
using MyCEP.API.IntegrationTest.Infrastructure.Fakes;

namespace MyCEP.API.IntegrationTest.Infrastructure
{
    /// <summary>
    /// Servidor de testes de integração que usa as configurações da Lambda para maior realismo.
    /// Este componente principal coordena TestServer, banco de dados e configurações,
    /// mantendo a mesma infraestrutura da aplicação real com apenas substituições mínimas para teste.
    /// </summary>
    public class IntegrationTestServer : IAsyncDisposable
    {
        private readonly TestDatabaseManager _databaseManager;
        private TestServer? _server;
        private HttpClient? _client;
        private WebApplicationFactory<Program>? _factory;
        private bool _isInitialized = false;

        public TestServer Server => _server ?? throw new InvalidOperationException("Server not initialized. Call InitializeAsync first.");
        public HttpClient Client => _client ?? throw new InvalidOperationException("Client not initialized. Call InitializeAsync first.");
        public IConfiguration Configuration => _databaseManager.Configuration;
        public string ConnectionString => _databaseManager.ConnectionString;

        public IntegrationTestServer(TestDatabaseManager databaseManager)
        {
            _databaseManager = databaseManager ?? throw new ArgumentNullException(nameof(databaseManager));
        }

        /// <summary>
        /// Inicializa o servidor de teste com toda a infraestrutura necessária
        /// </summary>
        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            // 1. Inicializar o banco de dados
            await _databaseManager.InitializeAsync();

            // 2. Criar o TestServer usando as configurações da Lambda
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

                    // Usar as configurações da Lambda como base
                    builder.UseConfiguration(Configuration);

                    // Configurar serviços para teste usando configuração customizada
                    builder.ConfigureServices(services =>
                    {
                        // Primeiro, configurar os controllers e serviços básicos
                        services.AddControllers();

                        // Agora registrar nossos serviços usando o mesmo método da Lambda
                        services.RegisterServicesToLambda(Configuration);

                        // Substituir apenas o ViaCep Gateway para usar o fake
                        services.RemoveAll(typeof(IViaCepApiGateway));
                        services.AddSingleton<IViaCepApiGateway, FakeViaCepApiGateway>();

                        // Configurar logging para testes
                        services.AddLogging(builder =>
                            builder.AddConsole()
                                   .SetMinimumLevel(LogLevel.Information)
                                   .AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning));
                    });

                    // Configurar a aplicação para usar as configurações da Lambda
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.Sources.Clear();
                        config.AddConfiguration(Configuration);
                    });
                });

            // 3. Obter instâncias do servidor e cliente
            _server = _factory.Server;
            _client = _factory.CreateClient();

            // 4. Garantir que o esquema do banco existe
            await EnsureDatabaseSchemaAsync();

            _isInitialized = true;
        }

        /// <summary>
        /// Garante que o esquema do banco de dados existe
        /// </summary>
        private async Task EnsureDatabaseSchemaAsync()
        {
            using var scope = Server.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<MyCepDbContext>();
            await dbContext.Database.EnsureCreatedAsync();
        }

        /// <summary>
        /// Limpa os dados do banco entre testes, mantendo o esquema
        /// </summary>
        public async Task CleanDatabaseAsync()
        {
            await _databaseManager.CleanDatabaseAsync();
        }

        /// <summary>
        /// Faz seed de dados de teste no banco
        /// </summary>
        public async Task SeedTestDataAsync()
        {
            await _databaseManager.SeedTestDataAsync();
        }

        /// <summary>
        /// Obtém uma instância de serviço do container de DI do servidor
        /// </summary>
        public T GetService<T>() where T : notnull
        {
            return Server.Services.GetRequiredService<T>();
        }

        /// <summary>
        /// Obtém uma instância de serviço do container de DI com escopo
        /// </summary>
        public T GetScopedService<T>() where T : notnull
        {
            using var scope = Server.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Executa uma ação com um serviço em escopo
        /// </summary>
        public async Task ExecuteWithScopedServiceAsync<T>(Func<T, Task> action) where T : notnull
        {
            using var scope = Server.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<T>();
            await action(service);
        }

        /// <summary>
        /// Executa uma função com um serviço em escopo e retorna resultado
        /// </summary>
        public async Task<TResult> ExecuteWithScopedServiceAsync<TService, TResult>(Func<TService, Task<TResult>> func) where TService : notnull
        {
            using var scope = Server.Services.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TService>();
            return await func(service);
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();
            _server?.Dispose();
            _factory?.Dispose();

            // O DatabaseManager é gerenciado pela Collection Fixture
            // Não precisamos dispor dele aqui
        }
    }
}
