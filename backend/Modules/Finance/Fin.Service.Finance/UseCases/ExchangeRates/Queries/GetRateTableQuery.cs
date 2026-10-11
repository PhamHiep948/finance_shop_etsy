using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Queries
{
    public sealed class GetRateTableQuery : IRequest<Response<List<FxMonthRowResponse>>>
    {
        public string? From { get; set; }
        public string? To { get; set; }
    }
}
