using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Commands
{
    public sealed class DeleteMonthlyRateCommand : IRequest<Response<bool>>
    {
        public string Month { get; set; } = string.Empty;
    }
}
