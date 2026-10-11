using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Commands
{
    public sealed class ConfigureEtsyAutoSyncCommand : IRequest<Response<EtsyAutoSyncResponse>>
    {
        public bool Enabled { get; set; }
        public int IntervalMinutes { get; set; }
    }
}
