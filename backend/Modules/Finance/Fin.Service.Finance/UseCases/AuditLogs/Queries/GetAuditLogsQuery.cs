using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Requests;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.AuditLogs.Queries
{
    public sealed class GetAuditLogsQuery : IRequest<Response<PagedResult<AuditLogResponse>>>
    {
        public AuditLogSearchRequest Search { get; set; } = new();
    }
}
