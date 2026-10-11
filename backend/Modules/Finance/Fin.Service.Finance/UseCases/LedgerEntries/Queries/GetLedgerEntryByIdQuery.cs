using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Queries
{
    public sealed class GetLedgerEntryByIdQuery : IRequest<Response<LedgerEntryResponse>>
    {
        public string Kind { get; set; } = string.Empty;
        public long Id { get; set; }
    }
}
