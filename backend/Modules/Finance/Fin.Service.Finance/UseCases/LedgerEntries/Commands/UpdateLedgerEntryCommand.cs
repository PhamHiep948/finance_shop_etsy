using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Requests;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Commands
{
    public sealed class UpdateLedgerEntryCommand : IRequest<Response<LedgerEntryResponse>>
    {
        public string Kind { get; set; } = string.Empty;
        public long Id { get; set; }
        public LedgerEntryRequest Payload { get; set; } = new();
    }
}
