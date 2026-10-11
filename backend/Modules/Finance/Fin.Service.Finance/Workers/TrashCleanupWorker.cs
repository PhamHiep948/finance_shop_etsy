using System.Data.Common;
using Fin.Service.Finance.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fin.Service.Finance.Workers
{
    public sealed class TrashCleanupWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TrashCleanupWorker> _logger;

        public TrashCleanupWorker(IServiceScopeFactory scopeFactory, ILogger<TrashCleanupWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var purged = await scope.ServiceProvider.GetRequiredService<TrashPurger>().PurgeExpiredAsync(stoppingToken);
                if (purged > 0)
                {
                    _logger.LogInformation("Removed {Count} entries that stayed in the trash too long", purged);
                }
            }
            catch (DbException e)
            {
                // The API must still start when MySQL is down; the trash page purges again on its next visit.
                _logger.LogWarning(e, "Could not clean up the trash on start-up (database not ready)");
            }
        }
    }
}
