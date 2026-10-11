using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Constants;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Trash.Queries.Handlers
{
    public sealed class GetTrashHandler : IRequestHandler<GetTrashQuery, Response<TrashResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly TrashPurger _purger;

        public GetTrashHandler(IFinanceUnitOfWork unitOfWork, TrashPurger purger)
        {
            _unitOfWork = unitOfWork;
            _purger = purger;
        }

        public async Task<Response<TrashResponse>> Handle(GetTrashQuery request, CancellationToken cancellationToken)
        {
            // Expired entries disappear as soon as someone looks at the trash, even if the app ran for weeks.
            await _purger.PurgeExpiredAsync(cancellationToken);
            return Response<TrashResponse>.Ok(new TrashResponse
            {
                RetentionDays = FinanceConstants.TrashRetentionDays,
                Items = await _unitOfWork.LedgerEntryRepository.GetDeletedResponsesAsync(cancellationToken),
            });
        }
    }
}
