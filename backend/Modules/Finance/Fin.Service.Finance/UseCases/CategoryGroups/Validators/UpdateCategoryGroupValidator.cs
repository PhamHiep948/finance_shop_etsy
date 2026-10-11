using FluentValidation;
using Fin.Service.Finance.Constants;
using Fin.Service.Finance.UseCases.CategoryGroups.Commands;

namespace Fin.Service.Finance.UseCases.CategoryGroups.Validators
{
    public sealed class UpdateCategoryGroupValidator : AbstractValidator<UpdateCategoryGroupCommand>
    {
        public UpdateCategoryGroupValidator()
        {
            // Name is optional on update (ticking categories sends only the id list).
            RuleFor(x => x.Name)
                .Must(name => name!.Trim().Length is > 0 and <= FinanceConstants.NameMaxLength)
                .When(x => x.Name is not null)
                .WithMessage("Tên nhóm phải có từ 1 đến 150 ký tự.");
        }
    }
}
