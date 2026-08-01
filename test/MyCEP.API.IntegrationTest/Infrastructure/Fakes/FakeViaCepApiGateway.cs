using System.Text.Json;
using MyCEP.API.Core.Shared.Infrastructure.Gateways;
using Refit;

namespace MyCEP.API.IntegrationTest.Infrastructure.Fakes
{
    /// <summary>
    /// Implementação fake do gateway ViaCEP para testes de integração.
    /// Simula diferentes cenários de resposta da API externa de forma controlada.
    /// </summary>
    public class FakeViaCepApiGateway : IViaCepApiGateway
    {
        private readonly Dictionary<string, string> _predefinedResponses;

        public FakeViaCepApiGateway()
        {
            _predefinedResponses = new Dictionary<string, string>
            {
                ["01001000"] = "success", // CEP válido que retorna sucesso
                ["00000000"] = "not_found", // CEP que retorna não encontrado
                ["12345678"] = "success_alternative", // CEP alternativo para testes
                ["99999999"] = "not_found", // CEP que sempre falha
                ["50000000"] = "server_error" // simula o ViaCEP fora do ar (5xx)
            };
        }

        public Task<ApiResponse<Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response.AddressResponse>> GetAddressByZipCodeAsync(
            string zipCode,
            CancellationToken cancellationToken = default)
        {
            var normalizedZipCode = NormalizeZipCode(zipCode);

            return _predefinedResponses.TryGetValue(normalizedZipCode, out var responseType)
                ? responseType switch
                {
                    "success" => CreateSuccessResponse(normalizedZipCode),
                    "success_alternative" => CreateAlternativeSuccessResponse(normalizedZipCode),
                    "not_found" => CreateNotFoundResponse(),
                    "server_error" => CreateApiResponse(HttpStatusCode.InternalServerError, null),
                    _ => CreateNotFoundResponse()
                }
                : CreateNotFoundResponse();
        }

        /// <summary>
        /// Normaliza o CEP removendo formatação
        /// </summary>
        private static string NormalizeZipCode(string? zipCode)
        {
            return zipCode?.Replace("-", "").Replace(" ", "").Trim() ?? string.Empty;
        }

        /// <summary>
        /// Cria resposta de sucesso padrão
        /// </summary>
        private static Task<ApiResponse<Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response.AddressResponse>> CreateSuccessResponse(string zipCode)
        {
            var addressResponse = new Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response.AddressResponse
            {
                PostalCode = FormatZipCode(zipCode),
                Street = "Praça da Sé",
                Complement = "lado ímpar",
                Neighborhood = "Sé",
                City = "São Paulo",
                State = "SP",
                IbgeCode = "3550308",
                GiaCode = "1004",
                Ddd = "11",
                SiafiCode = "7107"
            };

            return CreateApiResponse(HttpStatusCode.OK, addressResponse);
        }

        /// <summary>
        /// Cria resposta de sucesso alternativa para testes variados
        /// </summary>
        private static Task<ApiResponse<Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response.AddressResponse>> CreateAlternativeSuccessResponse(string zipCode)
        {
            var addressResponse = new Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response.AddressResponse
            {
                PostalCode = FormatZipCode(zipCode),
                Street = "Rua da Consolação",
                Complement = "até 99 - lado ímpar",
                Neighborhood = "Consolação",
                City = "São Paulo",
                State = "SP",
                IbgeCode = "3550308",
                GiaCode = "1004",
                Ddd = "11",
                SiafiCode = "7107"
            };

            return CreateApiResponse(HttpStatusCode.OK, addressResponse);
        }

        /// <summary>
        /// Cria resposta de CEP não encontrado
        /// </summary>
        private static Task<ApiResponse<Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response.AddressResponse>> CreateNotFoundResponse()
        {
            return CreateApiResponse(HttpStatusCode.NotFound, null);
        }

        /// <summary>
        /// Cria uma resposta da API
        /// </summary>
        private static Task<ApiResponse<Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response.AddressResponse>> CreateApiResponse(
            HttpStatusCode statusCode,
            Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response.AddressResponse? content)
        {
            var httpResponse = new HttpResponseMessage(statusCode);

            if (content != null)
            {
                var json = JsonSerializer.Serialize(content);
                httpResponse.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }
            else
            {
                httpResponse.Content = new StringContent("CEP não encontrado");
            }

            var apiResponse = new ApiResponse<Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response.AddressResponse>(
                httpResponse,
                content!,
                new RefitSettings()
            );

            return Task.FromResult(apiResponse);
        }

        /// <summary>
        /// Formata o CEP com hífen
        /// </summary>
        private static string FormatZipCode(string zipCode)
        {
            return zipCode.Length == 8
                ? $"{zipCode[..5]}-{zipCode[5..]}"
                : zipCode;
        }

        /// <summary>
        /// Adiciona um CEP customizado para teste
        /// </summary>
        public void AddCustomResponse(string zipCode, string responseType)
        {
            var normalized = NormalizeZipCode(zipCode);
            _predefinedResponses[normalized] = responseType;
        }

        /// <summary>
        /// Remove um CEP customizado
        /// </summary>
        public void RemoveCustomResponse(string zipCode)
        {
            var normalized = NormalizeZipCode(zipCode);
            _predefinedResponses.Remove(normalized);
        }
    }
}
