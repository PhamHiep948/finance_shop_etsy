using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Settings.Queries.Handlers
{
    public sealed class GetSettingsHandler : IRequestHandler<GetSettingsQuery, Response<SettingsResponse>>
    {
        private readonly PeriodLockPolicy _periodLock;

        public GetSettingsHandler(PeriodLockPolicy periodLock)
        {
            _periodLock = periodLock;
        }

        public Task<Response<SettingsResponse>> Handle(GetSettingsQuery request, CancellationToken cancellationToken)
        {
            return Task.FromResult(Response<SettingsResponse>.Ok(new SettingsResponse { PeriodLock = _periodLock.ToResponse() }));
        }
    }
}
