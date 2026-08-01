namespace MyCEP.API.Core.Shared.Infrastructure.CrossCutting.UnitofWork
{
    public interface IUnitOfWork
    {
        Task BeginTransaction();
        Task Rollback();
        Task<int> Commit(CancellationToken cancellationToken = default);
    }
}
