using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers
{
    [ApiController]
    [ApiVersionNeutral]
    [Route("health")]
    public sealed class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get() => Ok(new { status = "healthy" });
    }
}
