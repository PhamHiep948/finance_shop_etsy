using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Requests;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Commands
{
    public sealed class CreateLedgerEntryCommand : IRequest<Response<LedgerEntryResponse>>
    {
        public string Kind { get; set; } = string.Empty;
        public LedgerEntryRequest Payload { get; set; } = new();
    }
}
