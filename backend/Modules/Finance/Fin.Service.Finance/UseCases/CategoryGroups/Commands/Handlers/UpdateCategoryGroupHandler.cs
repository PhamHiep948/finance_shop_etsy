using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Commands.Handlers
{
    public sealed class UpdateCategoryGroupHandler : IRequestHandler<UpdateCategoryGroupCommand, Response<bool>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public UpdateCategoryGroupHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<bool>> Handle(UpdateCategoryGroupCommand request, CancellationToken cancellationToken)
        {
            var group = await _unitOfWork.CategoryGroupRepository.GetByIdAsync(request.Id, cancellationToken);
            if (group is null)
            {
                return Response<bool>.Fail(ErrorCodeEnum.GRP_ERR_001);
            }

            if (request.Name is not null)
            {
                var name = request.Name.Trim();
                if (await _unitOfWork.CategoryGroupRepository.NameExistsAsync(name, group.Id, cancellationToken))
                {
                    return Response<bool>.Fail(ErrorCodeEnum.GRP_ERR_002, $"Nhóm \"{name}\" đã tồn tại.");
                }
                group.Name = name;
            }
            if (request.CategoryIds is not null)
            {
                group.SetCategories(request.CategoryIds);
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return Response<bool>.Ok(true);
        }
    }
}
