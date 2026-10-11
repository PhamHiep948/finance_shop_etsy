using System.Globalization;
using Fin.Domain.Finance.Constants;
using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Contexts;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Model.Dto.Finance.Requests;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Finance.Repositories
{
    public sealed class AuditLogRepository : IAuditLogRepository
    {
        private const string EntryFilter = "ENTRY";
        private const string AttachmentFilter = "ATTACH";

        private readonly FinanceContext _dbContext;

        public AuditLogRepository(FinanceContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<(List<AuditLog> Items, long Total)> SearchAsync(AuditLogSearchRequest request, CancellationToken cancellationToken)
        {
            var query = _dbContext.AuditLogs.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.EntityType))
            {
                query = request.EntityType == EntryFilter
                    ? query.Where(x => x.EntityType == LedgerKinds.Income || x.EntityType == LedgerKinds.Expense)
                    : query.Where(x => x.EntityType == request.EntityType);
            }
            if (!string.IsNullOrWhiteSpace(request.Action))
            {
                query = request.Action == AttachmentFilter
                    ? query.Where(x => x.Action == AuditActions.AttachAdd || x.Action == AuditActions.AttachDelete)
                    : query.Where(x => x.Action == request.Action);
            }
            if (request.EntityId is not null)
            {
                query = query.Where(x => x.EntityId == request.EntityId);
            }
            if (!string.IsNullOrWhiteSpace(request.Search))
            {
                var search = request.Search.Trim();
                query = query.Where(x => x.Label.Contains(search) || (x.Detail != null && x.Detail.Contains(search)));
            }
            if (TryParseDate(request.DateFrom, out var from))
            {
                var start = from.ToDateTime(TimeOnly.MinValue);
                query = query.Where(x => x.CreatedAt >= start);
            }
            if (TryParseDate(request.DateTo, out var to))
            {
                var end = to.AddDays(1).ToDateTime(TimeOnly.MinValue);
                query = query.Where(x => x.CreatedAt < end);
            }

            var total = await query.LongCountAsync(cancellationToken);
            var items = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);
            return (items, total);
        }

        public void Add(AuditLog log) => _dbContext.AuditLogs.Add(log);

        private static bool TryParseDate(string? value, out DateOnly date) =>
            DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
