using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Trash.Commands
{
    public sealed class RestoreEntryCommand : IRequest<Response<LedgerEntryResponse>>
    {
        public long Id { get; set; }
    }
}
