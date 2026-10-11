using Microsoft.EntityFrameworkCore;

namespace Fin.UnitOfWork
{
    public class BaseUnitOfWork : IUnitOfWork
    {
        public BaseUnitOfWork(DbContext context) => Context = context;

        protected DbContext Context { get; }

        public virtual async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await Context.SaveChangesAsync(cancellationToken);
        }

        public virtual async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            if (Context.Database.CurrentTransaction is not null)
            {
                var nested = await action();
                await Context.SaveChangesAsync(cancellationToken);
                return nested;
            }

            var strategy = Context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await Context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var result = await action();
                    await Context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
    }
}
