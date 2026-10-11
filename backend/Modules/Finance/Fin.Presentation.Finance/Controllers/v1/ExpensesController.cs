using Asp.Versioning;
using Fin.Domain.Finance.Constants;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Presentation.Finance.Controllers.v1
{
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/expenses")]
    public sealed class ExpensesController : LedgerEntriesControllerBase
    {
        protected override string Kind => LedgerKinds.Expense;
    }
}
