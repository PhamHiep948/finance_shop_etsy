using Fin.Domain.Finance.Constants;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Categories.Commands.Handlers
{
    public sealed class RenameCategoryHandler : IRequestHandler<RenameCategoryCommand, Response<CategoryResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly AuditTrail _auditTrail;

        public RenameCategoryHandler(IFinanceUnitOfWork unitOfWork, AuditTrail auditTrail)
        {
            _unitOfWork = unitOfWork;
            _auditTrail = auditTrail;
        }

        public async Task<Response<CategoryResponse>> Handle(RenameCategoryCommand request, CancellationToken cancellationToken)
        {
            var kind = CategoryKinds.FromRoute(request.Type);
            var category = kind is null ? null : await _unitOfWork.CategoryRepository.GetByIdAsync(request.Id, kind, cancellationToken);
            if (kind is null || category is null)
            {
                return Response<CategoryResponse>.Fail(ErrorCodeEnum.CAT_ERR_001);
            }

            var name = request.Name!.Trim();
            if (await _unitOfWork.CategoryRepository.NameExistsAsync(kind, name, category.Id, cancellationToken))
            {
                return Response<CategoryResponse>.Fail(ErrorCodeEnum.CAT_ERR_002, $"Loại \"{name}\" đã tồn tại.");
            }

            var oldName = category.Name;
            category.Name = name;
            if (oldName != name)
            {
                _auditTrail.Log(AuditActions.EntityCategory, category.Id, AuditActions.Update, name, CategoryKinds.Label(kind),
                    AuditTrail.NameChange(oldName, name));
            }
            await _unitOfWork.CommitAsync(cancellationToken);

            return Response<CategoryResponse>.Ok(new CategoryResponse { Id = category.Id, Name = name });
        }
    }
}
