using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.ExchangeRates.Commands.Handlers
{
    public sealed class DeleteMonthlyRateHandler : IRequestHandler<DeleteMonthlyRateCommand, Response<bool>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public DeleteMonthlyRateHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<bool>> Handle(DeleteMonthlyRateCommand request, CancellationToken cancellationToken)
        {
            var existing = await _unitOfWork.FxMonthlyRateRepository.GetAsync(request.Month, cancellationToken);
            if (existing is null)
            {
                return Response<bool>.Fail(ErrorCodeEnum.FX_ERR_003);
            }

            _unitOfWork.FxMonthlyRateRepository.Remove(existing);
            await _unitOfWork.CommitAsync(cancellationToken);
            return Response<bool>.Ok(true);
        }
    }
}
