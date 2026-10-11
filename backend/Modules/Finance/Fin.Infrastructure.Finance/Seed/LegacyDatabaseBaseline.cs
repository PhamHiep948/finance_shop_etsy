using Fin.Infrastructure.Finance.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Finance.Seed
{
    public sealed class LegacyDatabaseBaseline
    {
        // The legacy backend created these on start-up; an older copy may still miss some of them.
        private static readonly string[] LegacyEnsureStatements =
        [
            """
            CREATE TABLE IF NOT EXISTS audit_logs (
             id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
             entity_type VARCHAR(20) NOT NULL,
             entity_id BIGINT UNSIGNED NOT NULL,
             action VARCHAR(20) NOT NULL,
             label VARCHAR(500) NOT NULL,
             detail VARCHAR(500) NULL,
             changes JSON NULL,
             created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
             INDEX idx_audit_created (created_at),
             INDEX idx_audit_entity (entity_type, entity_id)
            ) ENGINE=InnoDB
            """,
            """
            CREATE TABLE IF NOT EXISTS attachments (
             id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
             entry_id BIGINT UNSIGNED NOT NULL,
             original_name VARCHAR(255) NOT NULL,
             stored_name VARCHAR(100) NOT NULL,
             content_type VARCHAR(150) NOT NULL,
             size_bytes BIGINT NOT NULL,
             created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
             INDEX idx_attachment_entry (entry_id),
             CONSTRAINT fk_attachment_entry FOREIGN KEY (entry_id) REFERENCES ledger_entries(id) ON DELETE CASCADE
            ) ENGINE=InnoDB
            """,
            """
            CREATE TABLE IF NOT EXISTS fx_monthly (
             month CHAR(7) PRIMARY KEY,
             eur_to_usd DECIMAL(12,6) NOT NULL,
             updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
            ) ENGINE=InnoDB
            """,
            """
            CREATE TABLE IF NOT EXISTS category_groups (
             id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
             name VARCHAR(150) NOT NULL,
             category_ids TEXT NOT NULL,
             created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
             UNIQUE KEY uq_group_name (name)
            ) ENGINE=InnoDB
            """,
            "ALTER TABLE ledger_entries ADD COLUMN IF NOT EXISTS source VARCHAR(20) NULL AFTER payment_method",
        ];

        private readonly FinanceContext _context;
        private readonly ILogger<LegacyDatabaseBaseline> _logger;

        public LegacyDatabaseBaseline(FinanceContext context, ILogger<LegacyDatabaseBaseline> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> ApplyAsync(CancellationToken cancellationToken)
        {
            var initialMigration = _context.Database.GetMigrations().FirstOrDefault();
            if (initialMigration is null || !await TableExistsAsync("ledger_entries", cancellationToken))
            {
                return false;
            }

            var applied = await _context.Database.GetAppliedMigrationsAsync(cancellationToken);
            if (applied.Any())
            {
                return false;
            }

            // The tables were created by the legacy backend (raw SQL, no EF history). Bring them to the exact shape of the
            // initial migration, then record that migration as applied so EF only runs the migrations that come after it.
            foreach (var statement in LegacyEnsureStatements)
            {
                await _context.Database.ExecuteSqlRawAsync(statement, cancellationToken);
            }

            var historyRepository = _context.GetService<IHistoryRepository>();
            await _context.Database.ExecuteSqlRawAsync(historyRepository.GetCreateIfNotExistsScript(), cancellationToken);
            var productVersion = typeof(DbContext).Assembly.GetName().Version?.ToString(3) ?? "9.0.0";
            await _context.Database.ExecuteSqlRawAsync(
                historyRepository.GetInsertScript(new HistoryRow(initialMigration, productVersion)),
                cancellationToken);

            _logger.LogInformation("Legacy database detected; baseline {Migration} recorded without re-creating tables", initialMigration);
            return true;
        }

        private async Task<bool> TableExistsAsync(string table, CancellationToken cancellationToken)
        {
            var count = await _context.Database
                .SqlQuery<int>($"SELECT COUNT(*) AS `Value` FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = {table}")
                .SingleAsync(cancellationToken);
            return count > 0;
        }
    }
}
