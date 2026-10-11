using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Trash.Commands.Handlers
{
    public sealed class PurgeEntryHandler : IRequestHandler<PurgeEntryCommand, Response<bool>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly TrashPurger _purger;

        public PurgeEntryHandler(IFinanceUnitOfWork unitOfWork, TrashPurger purger)
        {
            _unitOfWork = unitOfWork;
            _purger = purger;
        }

        public async Task<Response<bool>> Handle(PurgeEntryCommand request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.LedgerEntryRepository.GetAnyAsync(request.Id, cancellationToken);
            if (entry is null || !entry.IsDeleted)
            {
                return Response<bool>.Fail(ErrorCodeEnum.TRS_ERR_001);
            }

            await _purger.PurgeAsync([entry], "Xóa vĩnh viễn khỏi thùng rác", cancellationToken);
            return Response<bool>.Ok(true);
        }
    }
}
