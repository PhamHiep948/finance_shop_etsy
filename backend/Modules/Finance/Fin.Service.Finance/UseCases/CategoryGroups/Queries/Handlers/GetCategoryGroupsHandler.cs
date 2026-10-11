using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Queries.Handlers
{
    public sealed class GetCategoryGroupsHandler : IRequestHandler<GetCategoryGroupsQuery, Response<List<CategoryGroupResponse>>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public GetCategoryGroupsHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<List<CategoryGroupResponse>>> Handle(GetCategoryGroupsQuery request, CancellationToken cancellationToken)
        {
            var groups = await _unitOfWork.CategoryGroupRepository.GetListAsync(cancellationToken);
            return Response<List<CategoryGroupResponse>>.Ok(groups
                .Select(g => new CategoryGroupResponse { Id = g.Id, Name = g.Name, CategoryIds = g.CategoryIds })
                .ToList());
        }
    }
}
