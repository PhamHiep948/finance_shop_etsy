using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Commands.Handlers
{
    public sealed class CreateCategoryGroupHandler : IRequestHandler<CreateCategoryGroupCommand, Response<IdResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public CreateCategoryGroupHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<IdResponse>> Handle(CreateCategoryGroupCommand request, CancellationToken cancellationToken)
        {
            var name = request.Name!.Trim();
            if (await _unitOfWork.CategoryGroupRepository.NameExistsAsync(name, null, cancellationToken))
            {
                return Response<IdResponse>.Fail(ErrorCodeEnum.GRP_ERR_002, $"Nhóm \"{name}\" đã tồn tại.");
            }

            var group = new CategoryGroup { Name = name };
            group.SetCategories(request.CategoryIds ?? []);
            _unitOfWork.CategoryGroupRepository.Add(group);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Response<IdResponse>.Ok(new IdResponse(group.Id));
        }
    }
}
