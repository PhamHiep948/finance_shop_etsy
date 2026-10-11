using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Commands
{
    public sealed class DisconnectEtsyCommand : IRequest<Response<bool>>
    {
    }
}
