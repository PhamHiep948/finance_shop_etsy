using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Attachments.Queries.Handlers
{
    public sealed class GetAttachmentsHandler : IRequestHandler<GetAttachmentsQuery, Response<List<AttachmentResponse>>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public GetAttachmentsHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<List<AttachmentResponse>>> Handle(GetAttachmentsQuery request, CancellationToken cancellationToken)
        {
            if (await _unitOfWork.LedgerEntryRepository.GetAsync(request.Kind, request.EntryId, cancellationToken) is null)
            {
                return Response<List<AttachmentResponse>>.Fail(ErrorCodeEnum.LED_ERR_001);
            }
            return Response<List<AttachmentResponse>>.Ok(await _unitOfWork.AttachmentRepository.GetByEntryAsync(request.EntryId, cancellationToken));
        }
    }
}
