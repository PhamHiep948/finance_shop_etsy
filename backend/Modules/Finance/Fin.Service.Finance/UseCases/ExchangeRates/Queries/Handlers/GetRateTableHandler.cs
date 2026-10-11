using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Queries.Handlers
{
    public sealed class GetRateTableHandler : IRequestHandler<GetRateTableQuery, Response<List<FxMonthRowResponse>>>
    {
        private const int MaxMonthsApart = 35;

        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly IEcbRateProvider _ecbRates;

        public GetRateTableHandler(IFinanceUnitOfWork unitOfWork, IEcbRateProvider ecbRates)
        {
            _unitOfWork = unitOfWork;
            _ecbRates = ecbRates;
        }

        public async Task<Response<List<FxMonthRowResponse>>> Handle(GetRateTableQuery request, CancellationToken cancellationToken)
        {
            MonthKey.TryParse(request.From, out var start);
            MonthKey.TryParse(request.To, out var end);
            if (end < start)
            {
                (start, end) = (end, start);
            }
            if ((end.Year - start.Year) * 12 + end.Month - start.Month > MaxMonthsApart)
            {
                return Response<List<FxMonthRowResponse>>.Fail(ErrorCodeEnum.FX_ERR_002);
            }

            var manual = await _unitOfWork.FxMonthlyRateRepository.GetAllAsync(cancellationToken);
            var ecb = await _ecbRates.GetMonthlyAveragesAsync(start, end.AddMonths(1).AddDays(-1), cancellationToken);
            var rows = new List<FxMonthRowResponse>();
            for (var month = end; month >= start; month = month.AddMonths(-1))
            {
                var key = MonthKey.Of(month);
                rows.Add(new FxMonthRowResponse
                {
                    Month = key,
                    Manual = manual.TryGetValue(key, out var typed) ? typed : null,
                    Ecb = ecb.TryGetValue(key, out var reference) ? reference : null,
                });
            }
            return Response<List<FxMonthRowResponse>>.Ok(rows);
        }
    }
}
