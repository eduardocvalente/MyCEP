using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace MyCEP.API.Core.API.Middleware
{
    public class ApiMiddleware
    {
        private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

        private readonly RequestDelegate _next;
        private readonly ILogger<ApiMiddleware> _logger;

        public ApiMiddleware(RequestDelegate next, ILogger<ApiMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                // O cliente abortou a requisição; não há resposta a produzir.
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted)
                    throw;

                var statusCode = ex switch
                {
                    ArgumentException => StatusCodes.Status400BadRequest,
                    UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                    _ => StatusCodes.Status500InternalServerError
                };

                _logger.LogError(ex, "Exceção não tratada em {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                var isServerError = statusCode >= StatusCodes.Status500InternalServerError;
                var problem = new ProblemDetails
                {
                    Status = statusCode,
                    // A mensagem de uma exceção inesperada pode carregar detalhe
                    // interno (connection string, caminho de arquivo, SQL); em 5xx
                    // só sai o texto genérico — o detalhe fica no log.
                    Title = isServerError ? "Erro interno no servidor." : ex.Message,
                    Instance = context.Request.Path
                };
                problem.Extensions["traceId"] = context.TraceIdentifier;

                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(problem, SerializerOptions));
            }
        }
    }
}
