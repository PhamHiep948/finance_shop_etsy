using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Constants;
using MediatR;

namespace Fin.Service.Finance.UseCases.LedgerEntries.Queries.Handlers
{
    public sealed class GetLedgerEntriesHandler : IRequestHandler<GetLedgerEntriesQuery, Response<PagedResult<LedgerEntryResponse>>>
    {
        private readonly IFinanceUnitOfWork _unitOfWork;

        public GetLedgerEntriesHandler(IFinanceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Response<PagedResult<LedgerEntryResponse>>> Handle(GetLedgerEntriesQuery request, CancellationToken cancellationToken)
        {
            var search = request.Search;
            search.Page = Math.Max(1, search.Page);
            search.PageSize = Math.Clamp(search.PageSize <= 0 ? FinanceConstants.DefaultPageSize : search.PageSize, 1, FinanceConstants.MaxPageSize);

            var (items, total) = await _unitOfWork.LedgerEntryRepository.SearchAsync(request.Kind, search, cancellationToken);
            return Response<PagedResult<LedgerEntryResponse>>.Ok(new PagedResult<LedgerEntryResponse>(items, search.Page, search.PageSize, total));
        }
    }
}
