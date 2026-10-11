using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Models;
using MediatR;

namespace Fin.Service.Finance.UseCases.Attachments.Commands
{
    public sealed class UploadAttachmentsCommand : IRequest<Response<List<AttachmentResponse>>>
    {
        public string Kind { get; set; } = string.Empty;
        public long EntryId { get; set; }
        public IReadOnlyList<AttachmentUpload> Files { get; set; } = [];
    }
}
