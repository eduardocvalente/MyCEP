using System.Collections.Concurrent;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Domain.Entities.Base;
using MyCEP.API.Core.Shared.Infrastructure.Schemas;

namespace MyCEP.API.Core.Shared.Infrastructure.Map
{
    public static class DomainToDatabaseMap
    {
        private static IDictionary<Type, Func<DomainBase, InfraBase>> _map = new ConcurrentDictionary<Type, Func<DomainBase, InfraBase>>();

        static DomainToDatabaseMap()
        {
            _map.Add(typeof(AddressEntity), (entity) =>
            {
                var domainEntity = entity as AddressEntity;
                return new AddressInfra
                {
                    Id = domainEntity!.Id,
                    Zipcode = domainEntity.Zipcode,
                    Street = domainEntity.Street,
                    Complement = domainEntity.Complement,
                    Neighborhood = domainEntity.Neighborhood,
                    City = domainEntity.City,
                    State = domainEntity.State,
                    IbgeCode = domainEntity.IbgeCode,
                    GiaCode = domainEntity.GiaCode,
                    Ddd = domainEntity.Ddd,
                    SiafiCode = domainEntity.SiafiCode,
                    CreatedAt = domainEntity.CreatedAt,
                    UpdatedAt = domainEntity.UpdatedAt,
                    DeletedAt = domainEntity.DeletedAt,
                };
            });
        }
        public static InfraBase Map(DomainBase entity)
        {
            return _map[entity.GetType()](entity);
        }
    }

}
