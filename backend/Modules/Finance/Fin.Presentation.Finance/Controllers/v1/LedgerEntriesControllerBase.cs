using Fin.Model.Dto.Finance.Requests;
using Fin.Presentation.Shared.Controllers;
using Fin.Service.Finance.Models;
using Fin.Service.Finance.UseCases.Attachments.Commands;
using Fin.Service.Finance.UseCases.Attachments.Queries;
using Fin.Service.Finance.UseCases.LedgerEntries.Commands;
using Fin.Service.Finance.UseCases.LedgerEntries.Queries;
using Fin.Infrastructure.Shared.ErrorCodes;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    public abstract class LedgerEntriesControllerBase : BaseApiController
    {
        protected abstract string Kind { get; }

        [HttpGet]
        public async Task<IActionResult> SearchAsync([FromQuery] LedgerSearchRequest request) =>
            Ok(await Mediator.Send(new GetLedgerEntriesQuery { Kind = Kind, Search = request }));

        [HttpGet("{id:long}")]
        public async Task<IActionResult> GetAsync(long id) =>
            Ok(await Mediator.Send(new GetLedgerEntryByIdQuery { Kind = Kind, Id = id }));

        [HttpPost]
        public async Task<IActionResult> CreateAsync(LedgerEntryRequest request) =>
            Ok(await Mediator.Send(new CreateLedgerEntryCommand { Kind = Kind, Payload = request }));

        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateAsync(long id, LedgerEntryRequest request) =>
            Ok(await Mediator.Send(new UpdateLedgerEntryCommand { Kind = Kind, Id = id, Payload = request }));

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteAsync(long id) =>
            Ok(await Mediator.Send(new DeleteLedgerEntryCommand { Kind = Kind, Id = id }));

        [HttpGet("{id:long}/attachments")]
        public async Task<IActionResult> GetAttachmentsAsync(long id) =>
            Ok(await Mediator.Send(new GetAttachmentsQuery { Kind = Kind, EntryId = id }));

        [HttpPost("{id:long}/attachments")]
        public async Task<IActionResult> UploadAttachmentsAsync(long id)
        {
            if (!Request.HasFormContentType)
            {
                return Ok(Fin.Infrastructure.Shared.Wrappers.Response<bool>.Fail(ErrorCodeEnum.ATT_ERR_003));
            }

            var form = await Request.ReadFormAsync(HttpContext.RequestAborted);
            var files = form.Files
                .Select(f => new AttachmentUpload(f.FileName, f.Length, f.OpenReadStream))
                .ToList();
            return Ok(await Mediator.Send(new UploadAttachmentsCommand { Kind = Kind, EntryId = id, Files = files }));
        }
    }
}
