using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Queries.Handlers
{
    public sealed class GetEtsyAuthorizeUrlHandler : IRequestHandler<GetEtsyAuthorizeUrlQuery, Response<string>>
    {
        private readonly IEtsyClient _etsyClient;

        public GetEtsyAuthorizeUrlHandler(IEtsyClient etsyClient)
        {
            _etsyClient = etsyClient;
        }

        public async Task<Response<string>> Handle(GetEtsyAuthorizeUrlQuery request, CancellationToken cancellationToken)
        {
            return Response<string>.Ok(await _etsyClient.CreateAuthorizeUrlAsync(cancellationToken));
        }
    }
}
