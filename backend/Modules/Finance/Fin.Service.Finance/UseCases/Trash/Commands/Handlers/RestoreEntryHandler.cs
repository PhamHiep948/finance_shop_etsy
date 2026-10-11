using Fin.Domain.Finance.Constants;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Trash.Commands.Handlers
{
    public sealed class RestoreEntryHandler : IRequestHandler<RestoreEntryCommand, Response<LedgerEntryResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly PeriodLockPolicy _periodLock;
        private readonly AuditTrail _auditTrail;

        public RestoreEntryHandler(IFinanceUnitOfWork unitOfWork, PeriodLockPolicy periodLock, AuditTrail auditTrail)
        {
            _unitOfWork = unitOfWork;
            _periodLock = periodLock;
            _auditTrail = auditTrail;
        }

        public async Task<Response<LedgerEntryResponse>> Handle(RestoreEntryCommand request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.LedgerEntryRepository.GetAnyAsync(request.Id, cancellationToken);
            if (entry is null || !entry.IsDeleted)
            {
                return Response<LedgerEntryResponse>.Fail(ErrorCodeEnum.TRS_ERR_001);
            }
            if (_periodLock.Check(entry.Date) is { } locked)
            {
                return Response<LedgerEntryResponse>.Fail(ErrorCodeEnum.COM_ERR_001, locked);
            }

            entry.Restore();
            _auditTrail.Log(entry.Kind, entry.Id, AuditActions.Restore, entry.Description, "Khôi phục từ thùng rác");

            if (entry.Kind == LedgerKinds.Income)
            {
                // Bring back the factory / shipping cost that was trashed together with this income.
                foreach (var linked in await _unitOfWork.LedgerEntryRepository.GetLinkedExpensesAsync(entry.Id, true, cancellationToken))
                {
                    if (_periodLock.IsLocked(linked.Date))
                    {
                        continue;
                    }
                    linked.Restore();
                    _auditTrail.Log(linked.Kind, linked.Id, AuditActions.Restore, linked.Description, $"Khôi phục cùng khoản thu #{entry.Id}");
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return Response<LedgerEntryResponse>.Ok((await _unitOfWork.LedgerEntryRepository.GetAnyResponseAsync(entry.Id, cancellationToken))!);
        }
    }
}
