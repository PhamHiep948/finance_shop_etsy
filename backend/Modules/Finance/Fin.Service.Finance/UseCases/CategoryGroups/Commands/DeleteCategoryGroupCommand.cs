using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Commands
{
    public sealed class DeleteCategoryGroupCommand : IRequest<Response<bool>>
    {
        public long Id { get; set; }
    }
}
