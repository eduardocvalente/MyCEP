using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;

namespace MyCEP.API.Core.Shared.Domain.Service.ViaCep
{
    public interface IViaCepService
    {
        Task<IAnalysisResult<AddressEntity>> GetAddressByZipCodeAsync(string zipCode, CancellationToken cancellationToken);
    }
}
