using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Requests;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Commands
{
    public sealed class SetIncomeCostCommand : IRequest<Response<LedgerEntryResponse?>>
    {
        public long IncomeId { get; set; }
        public IncomeCostRequest Payload { get; set; } = new();
    }
}
