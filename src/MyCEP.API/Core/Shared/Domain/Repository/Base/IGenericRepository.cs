using MyCEP.API.Core.Shared.Domain.Entities.Base;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;

namespace MyCEP.API.Core.Shared.Domain.Repository.Base
{
    public interface IGenericRepository<TEntity, TInfraEntity>
        where TEntity : DomainBase
        where TInfraEntity : InfraBase
    {
        Task<IAnalysisResult> CreateAsync(TEntity entity, CancellationToken cancellationToken = default);
    }
}
