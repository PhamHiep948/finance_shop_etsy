using Fin.Infrastructure.Finance.Contexts;
using Fin.Infrastructure.Finance.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fin.Migration
{
    public sealed class MigrationRunner
    {
        private readonly FinanceContext _context;
        private readonly LegacyDatabaseBaseline _baseline;
        private readonly FinanceSeeder _seeder;
        private readonly ILogger<MigrationRunner> _logger;

        public MigrationRunner(FinanceContext context, LegacyDatabaseBaseline baseline, FinanceSeeder seeder, ILogger<MigrationRunner> logger)
        {
            _context = context;
            _baseline = baseline;
            _seeder = seeder;
            _logger = logger;
        }

        public async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            // This runner is the only place that changes the schema; the API never migrates on start-up.
            await _baseline.ApplyAsync(cancellationToken);

            var pending = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
            _logger.LogInformation("Pending migrations for FinanceContext: {Count} {Names}", pending.Count, string.Join(", ", pending));
            await _context.Database.MigrateAsync(cancellationToken);

            await _seeder.SeedAsync(cancellationToken);
            _logger.LogInformation("Database is up to date");
            return 0;
        }
    }
}
