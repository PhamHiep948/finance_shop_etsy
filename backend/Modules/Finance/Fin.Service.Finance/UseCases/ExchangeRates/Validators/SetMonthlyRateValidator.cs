using FluentValidation;
using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Extensions;
using Fin.Service.Finance.Services;
using Fin.Service.Finance.UseCases.ExchangeRates.Commands;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Validators
{
    public sealed class SetMonthlyRateValidator : AbstractValidator<SetMonthlyRateCommand>
    {
        public SetMonthlyRateValidator()
        {
            RuleFor(x => x.Month).Must(m => MonthKey.TryParse(m, out _)).WithMessage(ErrorCodeEnum.FX_ERR_001.GetDescription());
            // 1 EUR is always close to 1 USD; this catches typos such as 0,0005 or 11513.
            RuleFor(x => x.EurToUsd)
                .InclusiveBetween(FxMonthlyRate.MinEurToUsd, FxMonthlyRate.MaxEurToUsd)
                .WithMessage("Tỷ giá 1 EUR = ? USD phải trong khoảng 0,5 – 3 (ví dụ 1,1593). Kiểm tra lại dấu phẩy / dấu chấm.");
        }
    }
}
