using Fin.Domain.Finance.Constants;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Attachments.Commands.Handlers
{
    public sealed class DeleteAttachmentHandler : IRequestHandler<DeleteAttachmentCommand, Response<bool>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly IAttachmentFileStore _fileStore;
        private readonly PeriodLockPolicy _periodLock;
        private readonly AuditTrail _auditTrail;

        public DeleteAttachmentHandler(IFinanceUnitOfWork unitOfWork, IAttachmentFileStore fileStore, PeriodLockPolicy periodLock, AuditTrail auditTrail)
        {
            _unitOfWork = unitOfWork;
            _fileStore = fileStore;
            _periodLock = periodLock;
            _auditTrail = auditTrail;
        }

        public async Task<Response<bool>> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
        {
            var attachment = await _unitOfWork.AttachmentRepository.GetByIdAsync(request.Id, cancellationToken);
            if (attachment is null)
            {
                return Response<bool>.Fail(ErrorCodeEnum.ATT_ERR_001);
            }

            var entry = await _unitOfWork.LedgerEntryRepository.GetAnyAsync(attachment.EntryId, cancellationToken);
            if (entry is not null && _periodLock.Check(entry.Date) is { } locked)
            {
                return Response<bool>.Fail(ErrorCodeEnum.COM_ERR_001, locked);
            }

            _unitOfWork.AttachmentRepository.Remove(attachment);
            if (entry is not null)
            {
                _auditTrail.Log(entry.Kind, entry.Id, AuditActions.AttachDelete, entry.Description, attachment.OriginalName);
            }
            await _unitOfWork.CommitAsync(cancellationToken);
            _fileStore.Delete(attachment.StoredName);
            return Response<bool>.Ok(true);
        }
    }
}
