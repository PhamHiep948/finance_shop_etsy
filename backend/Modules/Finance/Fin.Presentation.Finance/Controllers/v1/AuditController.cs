using Asp.Versioning;
using Fin.Model.Dto.Finance.Requests;
using Fin.Presentation.Shared.Controllers;
using Fin.Service.Finance.UseCases.AuditLogs.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/audit")]
    public sealed class AuditController : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> SearchAsync([FromQuery] AuditLogSearchRequest request) =>
            Ok(await Mediator.Send(new GetAuditLogsQuery { Search = request }));
    }
}
