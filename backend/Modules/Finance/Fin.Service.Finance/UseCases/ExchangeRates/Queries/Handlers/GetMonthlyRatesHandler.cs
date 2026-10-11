using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Queries.Handlers
{
    public sealed class GetMonthlyRatesHandler : IRequestHandler<GetMonthlyRatesQuery, Response<List<FxMonthlyResponse>>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public GetMonthlyRatesHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<List<FxMonthlyResponse>>> Handle(GetMonthlyRatesQuery request, CancellationToken cancellationToken)
        {
            var rates = await _unitOfWork.FxMonthlyRateRepository.GetAllAsync(cancellationToken);
            return Response<List<FxMonthlyResponse>>.Ok(rates.Select(p => new FxMonthlyResponse { Month = p.Key, EurToUsd = p.Value }).ToList());
        }
    }
}
