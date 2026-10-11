using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Commands.Handlers
{
    public sealed class DeleteCategoryGroupHandler : IRequestHandler<DeleteCategoryGroupCommand, Response<bool>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public DeleteCategoryGroupHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<bool>> Handle(DeleteCategoryGroupCommand request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.CategoryGroupRepository.GetByIdAsync(request.Id, cancellationToken);
            if (group is null)
            {
                return Response<bool>.Fail(ErrorCodeEnum.GRP_ERR_001);
            }

            _unitOfWork.CategoryGroupRepository.Remove(group);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Response<bool>.Ok(true);
        }
    }
}
