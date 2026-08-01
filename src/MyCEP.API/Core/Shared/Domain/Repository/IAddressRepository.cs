using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;

namespace MyCEP.API.Core.Shared.Domain.Repository
{
    public interface IAddressRepository
    {
        Task<IAnalysisResult<AddressEntity>> GetAddressZipCode(string zipCode, CancellationToken cancellationToken = default);
    }
}
