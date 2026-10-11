using Fin.Domain.Finance.Entities;
using Fin.Model.Dto.Finance.Requests;

namespace Fin.Infrastructure.Finance.Interfaces
{
    public interface IAuditLogRepository
    {
        Task<(List<AuditLog> Items, long Total)> SearchAsync(AuditLogSearchRequest request, CancellationToken cancellationToken);

        void Add(AuditLog log);
    }
}
