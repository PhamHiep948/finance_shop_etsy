using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Attachments.Queries
{
    public sealed class GetAttachmentsQuery : IRequest<Response<List<AttachmentResponse>>>
    {
        public string Kind { get; set; } = string.Empty;
        public long EntryId { get; set; }
    }
}
