using Fin.Domain.Finance.Constants;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Categories.Queries.Handlers
{
    public sealed class GetCategoriesHandler : IRequestHandler<GetCategoriesQuery, Response<List<CategoryResponse>>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public GetCategoriesHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<List<CategoryResponse>>> Handle(GetCategoriesQuery request, CancellationToken cancellationToken)
        {
            var kind = CategoryKinds.FromRoute(request.Type);
            if (kind is null)
            {
                return Response<List<CategoryResponse>>.Fail(ErrorCodeEnum.CAT_ERR_001);
            }
            return Response<List<CategoryResponse>>.Ok(await _unitOfWork.CategoryRepository.GetListAsync(kind, cancellationToken));
        }
    }
}
