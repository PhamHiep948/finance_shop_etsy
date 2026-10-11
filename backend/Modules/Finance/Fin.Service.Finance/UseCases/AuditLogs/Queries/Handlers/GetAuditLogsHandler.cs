using System.Text.Json;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Constants;
using MediatR;

namespace Fin.Service.Finance.UseCases.AuditLogs.Queries.Handlers
{
    public sealed class GetAuditLogsHandler : IRequestHandler<GetAuditLogsQuery, Response<PagedResult<AuditLogResponse>>>
    {
        private const int DefaultPageSize = 30;

        private readonly IFinanceUnitOfWork _unitOfWork;

        public GetAuditLogsHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<PagedResult<AuditLogResponse>>> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
        {
            var search = request.Search;
            search.Page = Math.Max(1, search.Page);
            search.PageSize = Math.Clamp(search.PageSize <= 0 ? DefaultPageSize : search.PageSize, 1, FinanceConstants.MaxPageSize);

            var (logs, total) = await _unitOfWork.AuditLogRepository.SearchAsync(search, cancellationToken);
            var items = logs.Select(log => new AuditLogResponse
            {
                Id = log.Id,
                EntityType = log.EntityType,
                EntityId = log.EntityId,
                Action = log.Action,
                Label = log.Label,
                Detail = log.Detail,
                Changes = ParseChanges(log.Changes),
                CreatedAt = log.CreatedAt,
            }).ToList();
            return Response<PagedResult<AuditLogResponse>>.Ok(new PagedResult<AuditLogResponse>(items, search.Page, search.PageSize, total));
        }

        private static JsonElement? ParseChanges(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                return doc.RootElement.Clone();
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}
