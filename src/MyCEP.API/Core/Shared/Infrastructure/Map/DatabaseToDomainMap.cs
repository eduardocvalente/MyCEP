using System.Collections.Concurrent;
using MyCEP.API.Core.Shared.Domain.Entities.Address;
using MyCEP.API.Core.Shared.Domain.Entities.Base;
using MyCEP.API.Core.Shared.Infrastructure.Schemas;

namespace MyCEP.API.Core.Shared.Infrastructure.Map
{
    public static class DatabaseToDomainMap
    {
        private static IDictionary<Type, Func<InfraBase, DomainBase>> _map = new ConcurrentDictionary<Type, Func<InfraBase, DomainBase>>();

        static DatabaseToDomainMap()
        {

            _map.Add(typeof(AddressInfra), (infraBaseEntity) =>
            {
                var infraEntity = infraBaseEntity as AddressInfra;

                return AddressEntity.Restore(
                    infraEntity!.Id,
                    infraEntity.Zipcode,
                    infraEntity.Street,
                    infraEntity.Complement,
                    infraEntity.Neighborhood,
                    infraEntity.City,
                    infraEntity.State,
                    infraEntity.IbgeCode,
                    infraEntity.GiaCode,
                    infraEntity.Ddd,
                    infraEntity.SiafiCode,
                    infraEntity.DeletedAt,
                    infraEntity.CreatedAt,
                    infraEntity.UpdatedAt
                );
            });
        }

        public static DomainBase Map(InfraBase infraBaseEntity)
        {
            return _map[infraBaseEntity.GetType()](infraBaseEntity);
        }
    }
}
