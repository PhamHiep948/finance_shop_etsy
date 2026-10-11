using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Commands.Handlers
{
    public sealed class SetMonthlyRateHandler : IRequestHandler<SetMonthlyRateCommand, Response<FxMonthlyResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public SetMonthlyRateHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<FxMonthlyResponse>> Handle(SetMonthlyRateCommand request, CancellationToken cancellationToken)
        {
            var rate = decimal.Round(request.EurToUsd, 6);
            var existing = await _unitOfWork.FxMonthlyRateRepository.GetAsync(request.Month, cancellationToken);
            if (existing is null)
            {
                _unitOfWork.FxMonthlyRateRepository.Add(new FxMonthlyRate { Month = request.Month, EurToUsd = rate });
            }
            else
            {
                existing.EurToUsd = rate;
            }
            await _unitOfWork.CommitAsync(cancellationToken);
            return Response<FxMonthlyResponse>.Ok(new FxMonthlyResponse { Month = request.Month, EurToUsd = rate });
        }
    }
}
