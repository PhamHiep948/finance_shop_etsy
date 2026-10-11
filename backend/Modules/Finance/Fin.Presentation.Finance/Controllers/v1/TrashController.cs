using Asp.Versioning;
using Fin.Presentation.Shared.Controllers;
using Fin.Service.Finance.UseCases.Trash.Commands;
using Fin.Service.Finance.UseCases.Trash.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/trash")]
    public sealed class TrashController : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetAsync() => Ok(await Mediator.Send(new GetTrashQuery()));

        [HttpPost("{id:long}/restore")]
        public async Task<IActionResult> RestoreAsync(long id) => Ok(await Mediator.Send(new RestoreEntryCommand { Id = id }));

        [HttpDelete("{id:long}")]
        public async Task<IActionResult> PurgeAsync(long id) => Ok(await Mediator.Send(new PurgeEntryCommand { Id = id }));

        [HttpDelete]
        public async Task<IActionResult> EmptyAsync() => Ok(await Mediator.Send(new EmptyTrashCommand()));
    }
}
