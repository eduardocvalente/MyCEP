using MyCEP.API.Core.Shared.Infrastructure.Entities.Gateways.ViaCep.Response;
using Refit;

namespace MyCEP.API.Core.Shared.Infrastructure.Gateways
{
    /// <summary>
    /// Interface para consumo da API ViaCEP usando Refit.
    /// </summary>
    public interface IViaCepApiGateway
    {
        /// <summary>
        /// Busca informações de endereço com base no CEP.
        /// </summary>
        /// <param name="zipcode">CEP do endereço a ser buscado.</param>
        /// <returns>Informações do endereço correspondente ao CEP.</returns>
        [Get("/{zipCode}/json/")]
        Task<ApiResponse<AddressResponse>> GetAddressByZipCodeAsync([AliasAs("zipcode")] string zipCode, CancellationToken cancellationToken = default);
    }
}