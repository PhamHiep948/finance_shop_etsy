using FluentValidation;
using Fin.Service.Finance.UseCases.LedgerEntries.Commands;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Validators
{
    public sealed class UpdateLedgerEntryValidator : AbstractValidator<UpdateLedgerEntryCommand>
    {
        public UpdateLedgerEntryValidator()
        {
            RuleFor(x => x.Payload).SetValidator(new LedgerEntryRequestValidator());
        }
    }
}
