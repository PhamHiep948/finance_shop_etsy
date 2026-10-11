using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Queries.Handlers
{
    public sealed class GetLedgerEntryByIdHandler : IRequestHandler<GetLedgerEntryByIdQuery, Response<LedgerEntryResponse>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public GetLedgerEntryByIdHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<LedgerEntryResponse>> Handle(GetLedgerEntryByIdQuery request, CancellationToken cancellationToken)
        {
            var entry = await _unitOfWork.LedgerEntryRepository.GetResponseAsync(request.Kind, request.Id, cancellationToken);
            return entry is null ? Response<LedgerEntryResponse>.Fail(ErrorCodeEnum.LED_ERR_001) : Response<LedgerEntryResponse>.Ok(entry);
        }
    }
}
