using Microsoft.EntityFrameworkCore;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Domain.Repository;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;
using MyCEP.API.Core.Shared.Infrastructure.Database.Postgres;
using MyCEP.API.Core.Shared.Infrastructure.Map;
using MyCEP.API.Core.Shared.Infrastructure.Schemas;

namespace MyCEP.API.Core.Shared.Infrastructure.Repository.Base
{
    public sealed class AddressRepository : IAddressRepository
    {
        private readonly MyCepDbContext _dbContext;

        public AddressRepository(MyCepDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IAnalysisResult<AddressEntity>> GetAddressZipCode(string zipCode, CancellationToken cancellationToken = default)
        {
            var address = await _dbContext.Set<AddressInfra>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Zipcode == zipCode, cancellationToken);

            if (address == null)
            {
                return ResultFactory.CreateFail<AddressEntity>(
                    NotificationErrors.Create(nameof(address), DefaultMessage.AddressNotFoundForZipCodeMessage),
                    ErrorType.BusinessError);
            }

            var addressResponse = DatabaseToDomainMap.Map(address) as AddressEntity;
            return ResultFactory.CreateSuccess(addressResponse!);
        }
    }
}
