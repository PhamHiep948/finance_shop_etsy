using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Commands.Handlers
{
    public sealed class ConfigureEtsyAutoSyncHandler : IRequestHandler<ConfigureEtsyAutoSyncCommand, Response<EtsyAutoSyncResponse>>
    {
        private readonly EtsyAutoSyncRunner _runner;

        public ConfigureEtsyAutoSyncHandler(EtsyAutoSyncRunner runner)
        {
            _runner = runner;
        }

        public Task<Response<EtsyAutoSyncResponse>> Handle(ConfigureEtsyAutoSyncCommand request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Response<EtsyAutoSyncResponse>.Ok(_runner.Configure(request.Enabled, request.IntervalMinutes)));
        }
    }
}
