using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Commands
{
    public sealed class CreateCategoryGroupCommand : IRequest<Response<IdResponse>>
    {
        public string? Name { get; set; }
        public List<long>? CategoryIds { get; set; }
    }
}
