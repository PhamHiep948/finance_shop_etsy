using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Trash.Commands
{
    public sealed class EmptyTrashCommand : IRequest<Response<PurgeResponse>>
    {
    }
}
