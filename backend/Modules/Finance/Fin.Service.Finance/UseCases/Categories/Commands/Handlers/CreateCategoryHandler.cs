using Fin.Domain.Finance.Constants;
using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Categories.Commands.Handlers
{
    public sealed class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, Response<CategoryResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly AuditTrail _auditTrail;

        public CreateCategoryHandler(IFinanceUnitOfWork unitOfWork, AuditTrail auditTrail)
        {
            _unitOfWork = unitOfWork;
            _auditTrail = auditTrail;
        }

        public async Task<Response<CategoryResponse>> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
        {
            var kind = CategoryKinds.FromRoute(request.Type);
            if (kind is null)
            {
                return Response<CategoryResponse>.Fail(ErrorCodeEnum.CAT_ERR_001);
            }

            var name = request.Name!.Trim();
            if (await _unitOfWork.CategoryRepository.NameExistsAsync(kind, name, null, cancellationToken))
            {
                return Response<CategoryResponse>.Fail(ErrorCodeEnum.CAT_ERR_002, $"Loại \"{name}\" đã tồn tại.");
            }

            var category = new Category { Kind = kind, Name = name };
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                _unitOfWork.CategoryRepository.Add(category);
                await _unitOfWork.CommitAsync(cancellationToken);
                _auditTrail.Log(AuditActions.EntityCategory, category.Id, AuditActions.Create, name, CategoryKinds.Label(kind));
                return true;
            }, cancellationToken);

            return Response<CategoryResponse>.Ok(new CategoryResponse { Id = category.Id, Name = name, Used = 0 });
        }
    }
}
