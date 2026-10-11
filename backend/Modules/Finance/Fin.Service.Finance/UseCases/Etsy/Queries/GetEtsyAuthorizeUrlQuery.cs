using Fin.Infrastructure.Shared.Wrappers;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Queries
{
    public sealed class GetEtsyAuthorizeUrlQuery : IRequest<Response<string>>
    {
    }
}
