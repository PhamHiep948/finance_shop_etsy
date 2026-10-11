using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Commands.Handlers
{
    public sealed class CompleteEtsyAuthorizationHandler : IRequestHandler<CompleteEtsyAuthorizationCommand, Response<bool>>
    {
        private readonly IEtsyClient _etsyClient;

        public CompleteEtsyAuthorizationHandler(IEtsyClient etsyClient)
        {
            _etsyClient = etsyClient;
        }

        public async Task<Response<bool>> Handle(CompleteEtsyAuthorizationCommand request, CancellationToken cancellationToken)
        {
            await _etsyClient.CompleteAuthorizationAsync(request.Code, request.State, cancellationToken);
            return Response<bool>.Ok(true);
        }
    }
}
