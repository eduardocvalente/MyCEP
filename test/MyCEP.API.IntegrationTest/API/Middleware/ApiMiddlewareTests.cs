using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using MyCEP.API.Core.API.Middleware;
using MyCEP.API.IntegrationTest.Base;

namespace MyCEP.API.IntegrationTest.API.Middleware;

public class ApiMiddlewareTests : IntegrationTestBase
{
    private HttpClient CreateClient(RequestDelegate handler)
    {
        var builder = new WebHostBuilder()
            .ConfigureServices(services => services.AddLogging())
            .Configure(app =>
            {
                app.UseMiddleware<ApiMiddleware>();
                app.Run(handler);
            });

        var server = new TestServer(builder);
        return server.CreateClient();
    }

    [Theory(DisplayName = "Deve tratar exceções e retornar ProblemDetails com o status correto")]
    [InlineData(typeof(ArgumentException), HttpStatusCode.BadRequest)]
    [InlineData(typeof(UnauthorizedAccessException), HttpStatusCode.Unauthorized)]
    [InlineData(typeof(InvalidOperationException), HttpStatusCode.InternalServerError)]
    public async Task ApiMiddleware_ShouldHandleExceptions(Type exceptionType, HttpStatusCode expectedStatus)
    {
        // Arrange
        HttpClient client = CreateClient(context =>
        {
            throw (Exception)Activator.CreateInstance(exceptionType, "Erro simulado!")!;
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(expectedStatus);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("status").GetInt32().Should().Be((int)expectedStatus);

        if (expectedStatus == HttpStatusCode.InternalServerError)
        {
            // Em 5xx a mensagem da exceção não pode vazar para o cliente.
            doc.RootElement.GetProperty("title").GetString().Should().Be("Erro interno no servidor.");
            json.Should().NotContain("Erro simulado!");
        }
        else
        {
            doc.RootElement.GetProperty("title").GetString().Should().Be("Erro simulado!");
        }
    }

    [Fact(DisplayName = "Deve passar requisição sem exceção")]
    public async Task ApiMiddleware_ShouldPassThrough_WhenNoException()
    {
        // Arrange
        HttpClient client = CreateClient(async context =>
        {
            context.Response.StatusCode = 200;
            await context.Response.WriteAsync("{\"ok\":true}");
        });

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("\"ok\":true");
    }
}
