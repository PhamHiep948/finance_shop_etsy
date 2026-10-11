using Asp.Versioning;
using Fin.Model.Dto.Finance.Requests;
using Fin.Presentation.Shared.Controllers;
using Fin.Service.Finance.UseCases.ExchangeRates.Commands;
using Fin.Service.Finance.UseCases.ExchangeRates.Queries;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/fx")]
    public sealed class FxController : BaseApiController
    {
        [HttpGet]
        public async Task<IActionResult> GetCurrentAsync() => Ok(await Mediator.Send(new GetCurrentRateQuery()));

        [HttpGet("monthly")]
        public async Task<IActionResult> GetMonthlyAsync() => Ok(await Mediator.Send(new GetMonthlyRatesQuery()));

        [HttpGet("months")]
        public async Task<IActionResult> GetTableAsync([FromQuery] string? from, [FromQuery] string? to) =>
            Ok(await Mediator.Send(new GetRateTableQuery { From = from, To = to }));

        [HttpPut("monthly/{month}")]
        public async Task<IActionResult> SetMonthlyAsync(string month, FxMonthlyRequest request) =>
            Ok(await Mediator.Send(new SetMonthlyRateCommand { Month = month, EurToUsd = request.EurToUsd }));

        [HttpDelete("monthly/{month}")]
        public async Task<IActionResult> DeleteMonthlyAsync(string month) =>
            Ok(await Mediator.Send(new DeleteMonthlyRateCommand { Month = month }));
    }
}
