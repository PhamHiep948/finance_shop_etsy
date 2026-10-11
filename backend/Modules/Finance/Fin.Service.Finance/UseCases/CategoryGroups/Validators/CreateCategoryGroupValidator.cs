using FluentValidation;
using Fin.Service.Finance.Constants;
using Fin.Service.Finance.UseCases.CategoryGroups.Commands;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Validators
{
    public sealed class CreateCategoryGroupValidator : AbstractValidator<CreateCategoryGroupCommand>
    {
        public CreateCategoryGroupValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => (name ?? string.Empty).Trim().Length is > 0 and <= FinanceConstants.NameMaxLength)
                .WithMessage("Tên nhóm phải có từ 1 đến 150 ký tự.");
        }
    }
}
