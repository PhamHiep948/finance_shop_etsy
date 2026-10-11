using Asp.Versioning;
using Fin.Domain.Finance.Constants;
using Fin.Model.Dto.Finance.Requests;
using Fin.Service.Finance.UseCases.LedgerEntries.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/incomes")]
    public sealed class IncomesController : LedgerEntriesControllerBase
    {
        protected override string Kind => LedgerKinds.Income;

        [HttpPut("{id:long}/cost")]
        public async Task<IActionResult> SetCostAsync(long id, IncomeCostRequest request) =>
            Ok(await Mediator.Send(new SetIncomeCostCommand { IncomeId = id, Payload = request }));
    }
}
