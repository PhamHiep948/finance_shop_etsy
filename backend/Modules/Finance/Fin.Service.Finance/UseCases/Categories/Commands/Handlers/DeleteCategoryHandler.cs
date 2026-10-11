using Fin.Domain.Finance.Constants;
using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Categories.Commands.Handlers
{
    public sealed class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, Response<bool>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly AuditTrail _auditTrail;

        public DeleteCategoryHandler(IFinanceUnitOfWork unitOfWork, AuditTrail auditTrail)
        {
            _unitOfWork = unitOfWork;
            _auditTrail = auditTrail;
        }

        public async Task<Response<bool>> Handle(DeleteCategoryCommand request, CancellationToken cancellationToken)
        {
            var kind = CategoryKinds.FromRoute(request.Type);
            var category = kind is null ? null : await _unitOfWork.CategoryRepository.GetByIdAsync(request.Id, kind, cancellationToken);
            if (kind is null || category is null)
            {
                return Response<bool>.Fail(ErrorCodeEnum.CAT_ERR_001);
            }

            if (kind == CategoryKinds.Scope)
            {
                return Response<bool>.Ok(await DeleteScopeAsync(category, cancellationToken));
            }

            var used = await _unitOfWork.CategoryRepository.CountEntriesAsync(category.Id, kind, false, cancellationToken);
            if (used > 0)
            {
                return Response<bool>.Fail(ErrorCodeEnum.CAT_ERR_003,
                    $"Loại này đang được dùng bởi {used} khoản. Hãy chuyển các khoản đó sang loại khác trước khi xóa.");
            }

            var inTrash = await _unitOfWork.CategoryRepository.CountEntriesAsync(category.Id, kind, true, cancellationToken);
            if (inTrash > 0)
            {
                return Response<bool>.Fail(ErrorCodeEnum.CAT_ERR_004,
                    $"Thùng rác còn {inTrash} khoản thuộc loại này. Hãy khôi phục hoặc xóa vĩnh viễn chúng trước.");
            }

            _unitOfWork.CategoryRepository.Remove(category);
            _auditTrail.Log(AuditActions.EntityCategory, category.Id, AuditActions.Delete, category.Name, CategoryKinds.Label(kind));
            await _unitOfWork.CommitAsync(cancellationToken);
            return Response<bool>.Ok(true);
        }

        // Scopes can be deleted freely: entries that used one (trash included) simply lose their scope.
        private async Task<bool> DeleteScopeAsync(Category scope, CancellationToken cancellationToken)
        {
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var cleared = await _unitOfWork.CategoryRepository.ClearScopeAsync(scope.Id, cancellationToken);
                _unitOfWork.CategoryRepository.Remove(scope);
                var detail = CategoryKinds.Label(CategoryKinds.Scope);
                _auditTrail.Log(AuditActions.EntityCategory, scope.Id, AuditActions.Delete, scope.Name,
                    cleared > 0 ? $"{detail} · {cleared} khoản chuyển về để trống" : detail);
                return true;
            }, cancellationToken);
        }
    }
}
