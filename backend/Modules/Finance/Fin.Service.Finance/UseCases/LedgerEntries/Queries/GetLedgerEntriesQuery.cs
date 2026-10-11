using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Requests;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Queries
{
    public sealed class GetLedgerEntriesQuery : IRequest<Response<PagedResult<LedgerEntryResponse>>>
    {
        public string Kind { get; set; } = string.Empty;
        public LedgerSearchRequest Search { get; set; } = new();
    }
}
