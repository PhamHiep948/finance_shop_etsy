using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.Trash.Commands
{
    public sealed class PurgeEntryCommand : IRequest<Response<bool>>
    {
        public long Id { get; set; }
    }
}
