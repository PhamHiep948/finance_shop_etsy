using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Commands.Handlers
{
    public sealed class DisconnectEtsyHandler : IRequestHandler<DisconnectEtsyCommand, Response<bool>>
    {
        private readonly IEtsyClient _etsyClient;

        public DisconnectEtsyHandler(IEtsyClient etsyClient)
        {
            _etsyClient = etsyClient;
        }

        public Task<Response<bool>> Handle(DisconnectEtsyCommand request, CancellationToken cancellationToken)
        {
            _etsyClient.Disconnect();
            return Task.FromResult(Response<bool>.Ok(true));
        }
    }
}
