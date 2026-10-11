using Fin.Domain.Finance.Constants;
using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Attachments.Commands.Handlers
{
    public sealed class UploadAttachmentsHandler : IRequestHandler<UploadAttachmentsCommand, Response<List<AttachmentResponse>>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly IAttachmentFileStore _fileStore;
        private readonly PeriodLockPolicy _periodLock;
        private readonly AuditTrail _auditTrail;

        public UploadAttachmentsHandler(IFinanceUnitOfWork unitOfWork, IAttachmentFileStore fileStore, PeriodLockPolicy periodLock, AuditTrail auditTrail)
        {
            _unitOfWork = unitOfWork;
            _fileStore = fileStore;
            _periodLock = periodLock;
            _auditTrail = auditTrail;
        }

        public async Task<Response<List<AttachmentResponse>>> Handle(UploadAttachmentsCommand request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.LedgerEntryRepository.GetAsync(request.Kind, request.EntryId, cancellationToken);
            if (entry is null)
            {
                return Response<List<AttachmentResponse>>.Fail(ErrorCodeEnum.LED_ERR_001);
            }
            if (_periodLock.Check(entry.Date) is { } locked)
            {
                return Response<List<AttachmentResponse>>.Fail(ErrorCodeEnum.COM_ERR_001, locked);
            }
            if (request.Files.Count == 0)
            {
                return Response<List<AttachmentResponse>>.Fail(ErrorCodeEnum.ATT_ERR_002);
            }
            if (request.Files.Count > AttachmentRules.MaxFilesPerUpload)
            {
                return Response<List<AttachmentResponse>>.Fail(ErrorCodeEnum.ATT_ERR_004, $"Mỗi lần chỉ tải tối đa {AttachmentRules.MaxFilesPerUpload} file.");
            }
            // Validate every file first so a bad file in the batch stores nothing.
            foreach (var file in request.Files)
            {
                if (AttachmentRules.Validate(file.FileName, file.Length) is { } error)
                {
                    return Response<List<AttachmentResponse>>.Fail(ErrorCodeEnum.ATT_ERR_004, error);
                }
            }

            var saved = new List<Attachment>();
            foreach (var file in request.Files)
            {
                await using var content = file.OpenReadStream();
                var storedName = await _fileStore.SaveAsync(content, Path.GetExtension(file.FileName), cancellationToken);
                var attachment = new Attachment
                {
                    EntryId = entry.Id,
                    OriginalName = AttachmentRules.DisplayName(file.FileName),
                    StoredName = storedName,
                    ContentType = AttachmentRules.ContentTypeOf(file.FileName),
                    SizeBytes = file.Length,
                };
                _unitOfWork.AttachmentRepository.Add(attachment);
                _auditTrail.Log(entry.Kind, entry.Id, AuditActions.AttachAdd, entry.Description, attachment.OriginalName);
                saved.Add(attachment);
            }
            await _unitOfWork.CommitAsync(cancellationToken);

            var savedIds = saved.Select(a => a.Id).ToHashSet();
            var all = await _unitOfWork.AttachmentRepository.GetByEntryAsync(entry.Id, cancellationToken);
            return Response<List<AttachmentResponse>>.Ok(all.Where(a => savedIds.Contains(a.Id)).ToList());
        }
    }
}
