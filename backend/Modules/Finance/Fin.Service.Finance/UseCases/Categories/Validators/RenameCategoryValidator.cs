using FluentValidation;
using Fin.Service.Finance.Constants;
using Fin.Service.Finance.UseCases.Categories.Commands;

namespace Fin.Service.Finance.UseCases.Categories.Validators
{
    public sealed class RenameCategoryValidator : AbstractValidator<RenameCategoryCommand>
    {
        public RenameCategoryValidator()
        {
            RuleFor(x => x.Name)
                .Must(name => (name ?? string.Empty).Trim().Length is > 0 and <= FinanceConstants.NameMaxLength)
                .WithMessage("Tên loại phải có từ 1 đến 150 ký tự.");
        }
    }
}
