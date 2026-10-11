using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Commands.Handlers
{
    public sealed class RunEtsyAutoSyncHandler : IRequestHandler<RunEtsyAutoSyncCommand, Response<EtsyAutoSyncResponse>>
    {
        private readonly EtsyAutoSyncRunner _runner;

        public RunEtsyAutoSyncHandler(EtsyAutoSyncRunner runner)
        {
            _runner = runner;
        }

        public async Task<Response<EtsyAutoSyncResponse>> Handle(RunEtsyAutoSyncCommand request, CancellationToken cancellationToken)
        {
            if (!_runner.IsReady)
            {
                return Response<EtsyAutoSyncResponse>.Fail(ErrorCodeEnum.ETS_ERR_001);
            }

            await _runner.RunOnceAsync(cancellationToken);
            return Response<EtsyAutoSyncResponse>.Ok(_runner.Status());
        }
    }
}
