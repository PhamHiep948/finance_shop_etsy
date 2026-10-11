using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Queries.Handlers
{
    public sealed class GetEtsyAutoSyncHandler : IRequestHandler<GetEtsyAutoSyncQuery, Response<EtsyAutoSyncResponse>>
    {
        private readonly EtsyAutoSyncRunner _runner;

        public GetEtsyAutoSyncHandler(EtsyAutoSyncRunner runner)
        {
            _runner = runner;
        }

        public Task<Response<EtsyAutoSyncResponse>> Handle(GetEtsyAutoSyncQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Response<EtsyAutoSyncResponse>.Ok(_runner.Status()));
        }
    }
}
