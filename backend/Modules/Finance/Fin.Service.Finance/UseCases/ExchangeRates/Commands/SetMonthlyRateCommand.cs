using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Commands
{
    public sealed class SetMonthlyRateCommand : IRequest<Response<FxMonthlyResponse>>
    {
        public string Month { get; set; } = string.Empty;
        public decimal EurToUsd { get; set; }
    }
}
