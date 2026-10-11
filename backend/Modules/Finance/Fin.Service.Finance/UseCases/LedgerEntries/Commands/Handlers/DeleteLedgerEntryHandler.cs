using Fin.Domain.Finance.Constants;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Commands.Handlers
{
    public sealed class DeleteLedgerEntryHandler : IRequestHandler<DeleteLedgerEntryCommand, Response<bool>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly PeriodLockPolicy _periodLock;
        private readonly AuditTrail _auditTrail;
        private readonly TimeProvider _timeProvider;

        public DeleteLedgerEntryHandler(IFinanceUnitOfWork unitOfWork, PeriodLockPolicy periodLock, AuditTrail auditTrail, TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _periodLock = periodLock;
            _auditTrail = auditTrail;
            _timeProvider = timeProvider;
        }

        public async Task<Response<bool>> Handle(DeleteLedgerEntryCommand request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.LedgerEntryRepository.GetAsync(request.Kind, request.Id, cancellationToken);
            if (entry is null)
            {
                return Response<bool>.Fail(ErrorCodeEnum.LED_ERR_001);
            }
            if (_periodLock.Check(entry.Date) is { } locked)
            {
                return Response<bool>.Fail(ErrorCodeEnum.COM_ERR_001, locked);
            }

            var now = _timeProvider.GetLocalNow().DateTime;
            entry.SoftDelete(now);
            _auditTrail.Log(entry.Kind, entry.Id, AuditActions.Delete, entry.Description, "Chuyển vào thùng rác");

            if (entry.Kind == LedgerKinds.Income)
            {
                // The factory / shipping cost of an income goes to the trash with it, unless its own period is locked.
                foreach (var linked in await _unitOfWork.LedgerEntryRepository.GetLinkedExpensesAsync(entry.Id, false, cancellationToken))
                {
                    if (_periodLock.IsLocked(linked.Date))
                    {
                        continue;
                    }
                    linked.SoftDelete(now);
                    _auditTrail.Log(linked.Kind, linked.Id, AuditActions.Delete, linked.Description, $"Xóa cùng khoản thu #{entry.Id}");
                }
            }

            await _unitOfWork.CommitAsync(cancellationToken);
            return Response<bool>.Ok(true);
        }
    }
}
