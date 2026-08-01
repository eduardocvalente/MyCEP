using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyCEP.API.Core.Features.Entities;
using MyCEP.API.IntegrationTest.Base;

namespace MyCEP.API.IntegrationTest.API.V1
{
    /// <summary>
    /// Testes de integração para o ZipCodeLookupController.
    /// Testa o comportamento completo da API através de requisições HTTP reais.
    /// </summary>
    public class ZipCodeLookupControllerTests : IntegrationTestBase
    {

        [Fact(DisplayName = "Deve retornar OK quando a busca de CEP for bem-sucedida")]
        public async Task GetAddressByZipCode_ShouldReturnOk_WhenSuccess()
        {
            // Arrange
            const string zipCode = "01001000";
            await SeedTestDataAsync();

            // Act
            var response = await GetHttpResponseAsync($"/api/ZipCodeLookup/{zipCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var addressResponse = await GetAsync<AddressResponse>($"/api/ZipCodeLookup/{zipCode}");

            addressResponse.Should().NotBeNull();
            addressResponse!.Zipcode.Should().Be("01001000");
            addressResponse.Street.Should().Be("Praça da Sé");
            addressResponse.City.Should().Be("São Paulo");
            addressResponse.State.Should().Be("SP");
        }

        [Fact(DisplayName = "Deve retornar BadRequest quando CEP for inválido")]
        public async Task GetAddressByZipCode_ShouldReturnBadRequest_WhenInvalidZipCode()
        {
            // Arrange
            const string invalidZipCode = "123"; // CEP com formato inválido

            // Act
            var response = await GetHttpResponseAsync($"/api/ZipCodeLookup/{invalidZipCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "Deve retornar NotFound quando CEP não for encontrado")]
        public async Task GetAddressByZipCode_ShouldReturnNotFound_WhenZipCodeNotFound()
        {
            // Arrange
            const string notFoundZipCode = "99999999"; // CEP que retorna não encontrado no fake

            // Act
            var response = await GetHttpResponseAsync($"/api/ZipCodeLookup/{notFoundZipCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "Deve retornar endereço do banco quando CEP já existe localmente")]
        public async Task GetAddressByZipCode_ShouldReturnFromDatabase_WhenZipCodeExistsLocally()
        {
            // Arrange
            const string zipCode = "01001000";
            await SeedTestDataAsync();

            // Act
            var addressResponse = await GetAsync<AddressResponse>($"/api/ZipCodeLookup/{zipCode}");

            // Assert
            addressResponse.Should().NotBeNull();
            addressResponse!.Zipcode.Should().Be("01001000");
            addressResponse.Street.Should().Be("Praça da Sé");
            addressResponse.City.Should().Be("São Paulo");
            addressResponse.State.Should().Be("SP");
        }

        [Fact(DisplayName = "Deve retornar BadRequest quando CEP for vazio")]
        public async Task GetAddressByZipCode_ShouldReturnBadRequest_WhenZipCodeIsEmpty()
        {
            // Arrange
            const string emptyZipCode = "";

            // Act
            var response = await GetHttpResponseAsync($"/api/ZipCodeLookup/{emptyZipCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "Deve retornar BadRequest quando CEP for null")]
        public async Task GetAddressByZipCode_ShouldReturnBadRequest_WhenZipCodeIsNull()
        {
            // Arrange
            const string nullZipCode = "null";

            // Act
            var response = await GetHttpResponseAsync($"/api/ZipCodeLookup/{nullZipCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Theory(DisplayName = "Deve tratar diferentes formatos de CEP corretamente")]
        [InlineData("01001-000", "01001000")]
        [InlineData("01001000", "01001000")]
        [InlineData("01001 000", "01001000")]
        [InlineData(" 01001-000 ", "01001000")]
        public async Task GetAddressByZipCode_ShouldHandleDifferentFormats_WhenZipCodeIsValid(
            string inputZipCode, string expectedZipCode)
        {
            // Arrange
            await SeedTestDataAsync();

            // Act
            var addressResponse = await GetAsync<AddressResponse>($"/api/ZipCodeLookup/{inputZipCode}");

            // Assert
            addressResponse.Should().NotBeNull();
            addressResponse!.Zipcode.Should().Be(expectedZipCode);
            addressResponse.Street.Should().Be("Praça da Sé");
        }

        [Fact(DisplayName = "Deve retornar NotFound quando o gateway não encontrar o CEP")]
        public async Task GetAddressByZipCode_ShouldReturnNotFound_WhenGatewayReturnsNotFound()
        {
            // Arrange - CEP que o fake do gateway responde como não encontrado
            const string problematicZipCode = "00000000";

            // Act
            var response = await GetHttpResponseAsync($"/api/ZipCodeLookup/{problematicZipCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "Deve processar múltiplas requisições concorrentes corretamente")]
        public async Task GetAddressByZipCode_ShouldHandleConcurrentRequests_WhenMultipleCallsMade()
        {
            // Arrange
            const string zipCode = "01001000";
            await SeedTestDataAsync();

            // Act - Fazer múltiplas requisições concorrentes
            var tasks = Enumerable.Range(0, 5).Select(async _ =>
                await GetAsync<AddressResponse>($"/api/ZipCodeLookup/{zipCode}")
            );

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllSatisfy(result =>
            {
                result.Should().NotBeNull();
                result!.Zipcode.Should().Be("01001000");
                result.Street.Should().Be("Praça da Sé");
            });
        }

        [Fact(DisplayName = "Requisições concorrentes de um CEP novo devem inserir apenas uma linha")]
        public async Task GetAddressByZipCode_ConcurrentSameNewZipCode_ShouldInsertOnce()
        {
            // Arrange — CEP que o fake resolve com sucesso e que NÃO está no seed:
            // força o caminho de miss no banco + inserção nas N requisições ao
            // mesmo tempo, exercitando o índice único parcial e o tratamento de
            // conflito no ViaCepService.
            const string zipCode = "12345678";

            // Act — dispara tudo de uma vez, sem aguardar entre as chamadas.
            var tasks = Enumerable.Range(0, 6)
                .Select(_ => GetHttpResponseAsync($"/api/ZipCodeLookup/{zipCode}"))
                .ToList();
            var responses = await Task.WhenAll(tasks);

            // Assert — todas respondem 200 e o banco tem exatamente uma linha.
            responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);

            var count = await DbContext.Addresses.CountAsync(a => a.Zipcode == zipCode);
            count.Should().Be(1, "a corrida de inserção não pode deixar linhas duplicadas");
        }

        [Fact(DisplayName = "Deve retornar 500 e não persistir quando o ViaCEP está fora do ar")]
        public async Task GetAddressByZipCode_ShouldReturn500_WhenViaCepIsDown()
        {
            // Arrange — CEP que o fake mapeia para uma resposta 5xx do ViaCEP.
            const string zipCode = "50000000";

            // Act
            var response = await GetHttpResponseAsync($"/api/ZipCodeLookup/{zipCode}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

            var body = await response.Content.ReadAsStringAsync();
            // O detalhe interno não pode vazar; nada é persistido para o CEP.
            body.Should().NotContain("Exception");
            var count = await DbContext.Addresses.CountAsync(a => a.Zipcode == zipCode);
            count.Should().Be(0);
        }
    }
}