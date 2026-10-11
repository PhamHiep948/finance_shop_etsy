using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Attachments.Queries.Handlers
{
    public sealed class GetAttachmentFileHandler : IRequestHandler<GetAttachmentFileQuery, Response<AttachmentFileResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly IAttachmentFileStore _fileStore;

        public GetAttachmentFileHandler(IFinanceUnitOfWork unitOfWork, IAttachmentFileStore fileStore)
        {
            _unitOfWork = unitOfWork;
            _fileStore = fileStore;
        }

        public async Task<Response<AttachmentFileResponse>> Handle(GetAttachmentFileQuery request, CancellationToken cancellationToken)
        {
            var attachment = await _unitOfWork.AttachmentRepository.GetByIdAsync(request.Id, cancellationToken);
            var path = attachment is null ? null : _fileStore.GetPath(attachment.StoredName);
            if (attachment is null || !File.Exists(path))
            {
                return Response<AttachmentFileResponse>.Fail(ErrorCodeEnum.ATT_ERR_001);
            }

            return Response<AttachmentFileResponse>.Ok(new AttachmentFileResponse
            {
                Path = path!,
                ContentType = attachment.ContentType,
                OriginalName = attachment.OriginalName,
                CanPreview = AttachmentRules.CanPreview(attachment.ContentType),
            });
        }
    }
}
