using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Commands
{
    public sealed class CompleteEtsyAuthorizationCommand : IRequest<Response<bool>>
    {
        public string Code { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }
}
