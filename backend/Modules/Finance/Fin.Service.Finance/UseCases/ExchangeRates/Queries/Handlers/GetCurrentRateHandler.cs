using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Queries.Handlers
{
    public sealed class GetCurrentRateHandler : IRequestHandler<GetCurrentRateQuery, Response<FxCurrentResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly IEcbRateProvider _ecbRates;
        private readonly TimeProvider _timeProvider;

        public GetCurrentRateHandler(IFinanceUnitOfWork unitOfWork, IEcbRateProvider ecbRates, TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _ecbRates = ecbRates;
            _timeProvider = timeProvider;
        }

        public async Task<Response<FxCurrentResponse>> Handle(GetCurrentRateQuery request, CancellationToken cancellationToken)
        {
            // A rate typed for the current month wins over the ECB reference rate.
            var month = MonthKey.Of(DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime));
            var manual = await _unitOfWork.FxMonthlyRateRepository.GetAsync(month, cancellationToken);
            if (manual is { EurToUsd: > 0 })
            {
                return Response<FxCurrentResponse>.Ok(new FxCurrentResponse
                {
                    EurToUsd = manual.EurToUsd,
                    UsdToEur = decimal.Round(1 / manual.EurToUsd, 6),
                    Date = month,
                    Auto = false,
                    Manual = true,
                });
            }

            var latest = await _ecbRates.GetLatestAsync(cancellationToken);
            return Response<FxCurrentResponse>.Ok(new FxCurrentResponse
            {
                EurToUsd = latest.EurToUsd,
                UsdToEur = decimal.Round(1 / latest.EurToUsd, 6),
                Date = latest.Date,
                Auto = latest.Auto,
                Manual = false,
            });
        }
    }
}
