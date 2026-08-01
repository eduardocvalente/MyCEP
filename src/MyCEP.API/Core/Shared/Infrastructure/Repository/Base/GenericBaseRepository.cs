using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using MyCEP.API.Core.Shared.Domain.Entities.Base;
using MyCEP.API.Core.Shared.Domain.Repository.Base;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.NotificationError;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.Result;
using MyCEP.API.Core.Shared.Infrastructure.Database.Postgres;
using MyCEP.API.Core.Shared.Infrastructure.Map;

namespace MyCEP.API.Core.Shared.Infrastructure.Repository.Base
{
    [ExcludeFromCodeCoverage]
    public class GenericBaseRepository<TEntity, TInfraBaseEntity> : IGenericRepository<TEntity, TInfraBaseEntity>
        where TEntity : DomainBase
        where TInfraBaseEntity : InfraBase
    {
        private const string IdKey = "id";
        private readonly ILogger<GenericBaseRepository<TEntity, TInfraBaseEntity>> _logger;
        private readonly DbSet<TInfraBaseEntity> _dbSet;

        public GenericBaseRepository(ILogger<GenericBaseRepository<TEntity, TInfraBaseEntity>> logger,
                                     MyCepDbContext dbContext)
        {
            _logger = logger;
            _dbSet = dbContext.Set<TInfraBaseEntity>();
        }

        public async Task<IAnalysisResult> CreateAsync(TEntity entity, CancellationToken cancellationToken = default)
        {
            try
            {
                if (entity == null)
                    return CreateErrorAboutEntityNull();

                var notificationErrors = entity.IsValid();
                if (notificationErrors.HaveError())
                {
                    _logger.LogInformation("Entidade inválida para criação: {Errors}", notificationErrors.Error);
                    return ResultFactory.CreateFail<TEntity>(notificationErrors, ErrorType.ValidationError);
                }

                var canCreated = entity.CreateDateToInit();
                if (!canCreated)
                {
                    notificationErrors = NotificationErrors.Create(IdKey, DefaultMessage.CanNotCreateEntity);
                    _logger.LogError(DefaultMessage.CanNotCreateEntity);
                    return ResultFactory.CreateFail<TEntity>(notificationErrors, ErrorType.ValidationError);
                }

                var result = await _dbSet.AddAsync((TInfraBaseEntity)DomainToDatabaseMap.Map(entity), cancellationToken);

                var baseEntity = (TEntity)DatabaseToDomainMap.Map(result.Entity);
                return ResultFactory.CreateSuccess(baseEntity);
            }
            catch (Exception ex)
            {
                return ResultFactory.CreateFail<TEntity>(ex);
            }
        }

        private IAnalysisResult<TEntity> CreateErrorAboutEntityNull()
        {
            var notificationErrors = NotificationErrors.Create(IdKey, DefaultMessage.EntityIsNull);
            _logger.LogInformation(DefaultMessage.EntityIsNull);
            return ResultFactory.CreateFail<TEntity>(notificationErrors, ErrorType.ValidationError);
        }
    }
}
