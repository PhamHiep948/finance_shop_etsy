using FluentValidation;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Extensions;
using Fin.Service.Finance.Services;
using Fin.Service.Finance.UseCases.ExchangeRates.Queries;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Validators
{
    public sealed class GetRateTableValidator : AbstractValidator<GetRateTableQuery>
    {
        public GetRateTableValidator()
        {
            RuleFor(x => x.From).Must(m => MonthKey.TryParse(m, out _)).WithMessage(ErrorCodeEnum.FX_ERR_001.GetDescription());
            RuleFor(x => x.To).Must(m => MonthKey.TryParse(m, out _)).WithMessage(ErrorCodeEnum.FX_ERR_001.GetDescription());
        }
    }
}
