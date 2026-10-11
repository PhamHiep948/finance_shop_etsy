using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Queries
{
    public sealed class GetMonthlyRatesQuery : IRequest<Response<List<FxMonthlyResponse>>>
    {
    }
}
