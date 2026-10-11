using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Commands
{
    public sealed class DeleteLedgerEntryCommand : IRequest<Response<bool>>
    {
        public string Kind { get; set; } = string.Empty;
        public long Id { get; set; }
    }
}
