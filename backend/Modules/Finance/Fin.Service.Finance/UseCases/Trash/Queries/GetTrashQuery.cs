using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Trash.Queries
{
    public sealed class GetTrashQuery : IRequest<Response<TrashResponse>>
    {
    }
}
