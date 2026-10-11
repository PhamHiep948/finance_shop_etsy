using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Trash.Commands.Handlers
{
    public sealed class EmptyTrashHandler : IRequestHandler<EmptyTrashCommand, Response<PurgeResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly TrashPurger _purger;

        public EmptyTrashHandler(IFinanceUnitOfWork unitOfWork, TrashPurger purger)
        {
            _unitOfWork = unitOfWork;
            _purger = purger;
        }

        public async Task<Response<PurgeResponse>> Handle(EmptyTrashCommand request, CancellationToken cancellationToken)
        {
            var deleted = await _unitOfWork.LedgerEntryRepository.GetDeletedAsync(null, cancellationToken);
            var purged = await _purger.PurgeAsync(deleted, "Làm trống thùng rác", cancellationToken);
            return Response<PurgeResponse>.Ok(new PurgeResponse(purged));
        }
    }
}
