using System.Text.Json.Serialization;

namespace MyCEP.API.Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response
{
    public record AddressResponse
    {
        [JsonPropertyName("logradouro")]
        public string? Street { get; set; }

        [JsonPropertyName("complemento")]
        public string? Complement { get; set; }

        [JsonPropertyName("bairro")]
        public string? Neighborhood { get; set; }

        [JsonPropertyName("localidade")]
        public string? City { get; set; }

        [JsonPropertyName("uf")]
        public string? State { get; set; }

        [JsonPropertyName("cep")]
        public string? PostalCode { get; set; }

        [JsonPropertyName("ibge")]
        public string? IbgeCode { get; set; }

        [JsonPropertyName("gia")]
        public string? GiaCode { get; set; }

        [JsonPropertyName("ddd")]
        public string? Ddd { get; set; }

        [JsonPropertyName("siafi")]
        public string? SiafiCode { get; set; }

        /// <summary>
        /// O ViaCEP responde HTTP 200 com {"erro": true} quando o CEP é
        /// bem-formado mas não existe. Sem mapear este campo, a resposta
        /// pareceria um endereço válido com todos os campos nulos.
        /// </summary>
        [JsonPropertyName("erro")]
        public bool Error { get; set; }
    }
}
