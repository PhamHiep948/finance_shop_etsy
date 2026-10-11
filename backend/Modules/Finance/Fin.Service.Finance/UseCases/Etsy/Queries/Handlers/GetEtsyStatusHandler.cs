using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Queries.Handlers
{
    public sealed class GetEtsyStatusHandler : IRequestHandler<GetEtsyStatusQuery, Response<EtsyStatusResponse>>
    {
        private readonly IEtsyClient _etsyClient;

        public GetEtsyStatusHandler(IEtsyClient etsyClient)
        {
            _etsyClient = etsyClient;
        }

        public async Task<Response<EtsyStatusResponse>> Handle(GetEtsyStatusQuery request, CancellationToken cancellationToken)
        {
            var status = await _etsyClient.GetStatusAsync(cancellationToken);
            return Response<EtsyStatusResponse>.Ok(new EtsyStatusResponse
            {
                Configured = status.Configured,
                KeyError = status.KeyError,
                Connected = status.Connected,
                ShopId = status.ShopId,
                RedirectUri = status.RedirectUri,
                Ready = status.Ready,
            });
        }
    }
}
