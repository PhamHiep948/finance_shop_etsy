using FluentValidation;
using Fin.Service.Finance.UseCases.LedgerEntries.Commands;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Validators
{
    public sealed class CreateLedgerEntryValidator : AbstractValidator<CreateLedgerEntryCommand>
    {
        public CreateLedgerEntryValidator()
        {
            RuleFor(x => x.Payload).SetValidator(new LedgerEntryRequestValidator());
        }
    }
}
