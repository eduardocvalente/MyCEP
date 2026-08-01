using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Storage;
using MyCEP.API.Core.Shared.Infrastructure.CrossCutting.UnitofWork;
using MyCEP.API.Core.Shared.Infrastructure.Database.Postgres;

namespace MyCEP.API.Core.Shared.Infrastructure.Repository.Base
{
    [ExcludeFromCodeCoverage]
    public sealed class PostgresUnitOfWork : IUnitOfWork
    {
        private readonly ILogger<PostgresUnitOfWork> _logger;
        private readonly MyCepDbContext _context;
        private IDbContextTransaction? _transaction;

        public PostgresUnitOfWork(ILogger<PostgresUnitOfWork> logger, MyCepDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task BeginTransaction()
        {
            try
            {
                // Se já existe uma transação ativa (EF ou nossa), não cria nova
                if (_transaction != null || _context.Database.CurrentTransaction != null)
                    return;

                _transaction = await _context.Database.BeginTransactionAsync();
                _logger.LogDebug("Transação aberta");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Houve um problema ao iniciar a transação");
                throw;
            }
        }

        public async Task<int> Commit(CancellationToken cancellationToken = default)
        {
            try
            {
                int rows = await _context.SaveChangesAsync(cancellationToken);
                if (_transaction != null)
                    await _transaction.CommitAsync(cancellationToken);

                return rows;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Houve um problema ao efetivar a transação");
                await Rollback();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task Rollback()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
