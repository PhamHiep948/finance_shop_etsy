using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Attachments.Queries
{
    public sealed class GetAttachmentFileQuery : IRequest<Response<AttachmentFileResponse>>
    {
        public long Id { get; set; }
    }
}
