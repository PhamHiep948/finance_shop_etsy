using Asp.Versioning;
using Fin.Presentation.Shared.Controllers;
using Fin.Service.Finance.UseCases.Attachments.Commands;
using Fin.Service.Finance.UseCases.Attachments.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/attachments")]
    public sealed class AttachmentsController : BaseApiController
    {
        [HttpGet("{id:long}")]
        public async Task<IActionResult> DownloadAsync(long id, [FromQuery] string? download)
        {
            var result = await Mediator.Send(new GetAttachmentFileQuery { Id = id });
            if (!result.Succeeded || result.Data is null)
            {
                return NotFound();
            }

            var file = result.Data;
            Response.Headers.XContentTypeOptions = "nosniff";
            // Images, PDF and plain text open in the browser; everything else (and ?download=1) is saved as a file.
            return download != "1" && file.CanPreview
                ? PhysicalFile(file.Path, file.ContentType)
                : PhysicalFile(file.Path, file.ContentType, file.OriginalName);
        }

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteAsync(long id) => Ok(await Mediator.Send(new DeleteAttachmentCommand { Id = id }));
    }
}
