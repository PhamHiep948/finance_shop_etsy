using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Queries
{
    public sealed class GetCategoryGroupsQuery : IRequest<Response<List<CategoryGroupResponse>>>
    {
    }
}
