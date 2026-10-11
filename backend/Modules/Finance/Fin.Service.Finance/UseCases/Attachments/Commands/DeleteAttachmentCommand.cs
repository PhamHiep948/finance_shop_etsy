using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.Attachments.Commands
{
    public sealed class DeleteAttachmentCommand : IRequest<Response<bool>>
    {
        public long Id { get; set; }
    }
}
