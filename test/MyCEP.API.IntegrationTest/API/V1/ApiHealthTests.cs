using FluentAssertions;
using MyCEP.API.IntegrationTest.Base;

namespace MyCEP.API.IntegrationTest.API.V1
{
    /// <summary>
    /// Testes de integração para verificar a saúde geral da API.
    /// Testa endpoints básicos e funcionamento da aplicação.
    /// </summary>
    public class ApiHealthTests : IntegrationTestBase
    {
        [Fact(DisplayName = "Deve retornar 404 para rota inexistente")]
        public async Task Get_ShouldReturn404_WhenRouteDoesNotExist()
        {
            // Act
            var response = await GetHttpResponseAsync("/api/nonexistent");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "Deve servir o Swagger UI na raiz")]
        public async Task Get_ShouldServeSwaggerUi_WhenAccessingRoot()
        {
            // O Swagger UI é servido na raiz de propósito (RoutePrefix vazio):
            // é a porta de entrada da demo pública.

            // Act
            var response = await GetHttpResponseAsync("/");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
        }

        [Fact(DisplayName = "Deve processar requisições sem body corretamente")]
        public async Task Get_ShouldProcessEmptyRequests_WhenValidEndpoint()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await GetHttpResponseAsync("/api/ZipCodeLookup/01001000");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact(DisplayName = "Deve retornar Content-Type correto")]
        public async Task Get_ShouldReturnCorrectContentType_WhenValidRequest()
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var response = await GetHttpResponseAsync("/api/ZipCodeLookup/01001000");

            // Assert
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }

        [Theory(DisplayName = "Deve lidar com diferentes métodos HTTP não suportados")]
        [InlineData("POST")]
        [InlineData("PUT")]
        [InlineData("DELETE")]
        [InlineData("PATCH")]
        public async Task ShouldReturn405_WhenUnsupportedHttpMethod(string httpMethod)
        {
            // Act
            var request = new HttpRequestMessage(new HttpMethod(httpMethod), "/api/ZipCodeLookup/01001000");
            var response = await Client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.MethodNotAllowed);
        }

        [Fact(DisplayName = "Deve processar requisições com headers customizados")]
        public async Task Get_ShouldProcessCustomHeaders_WhenValidRequest()
        {
            // Arrange
            await SeedTestDataAsync();
            Client.DefaultRequestHeaders.Add("X-Custom-Header", "test-value");

            // Act
            var response = await GetHttpResponseAsync("/api/ZipCodeLookup/01001000");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
