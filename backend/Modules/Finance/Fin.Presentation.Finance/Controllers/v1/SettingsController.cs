using Asp.Versioning;
using Fin.Presentation.Shared.Controllers;
using Fin.Service.Finance.UseCases.Settings.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/settings")]
    public sealed class SettingsController : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetAsync() => Ok(await Mediator.Send(new GetSettingsQuery()));
    }
}
