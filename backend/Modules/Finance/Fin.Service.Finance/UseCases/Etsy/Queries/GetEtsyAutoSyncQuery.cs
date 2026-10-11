using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Queries
{
    public sealed class GetEtsyAutoSyncQuery : IRequest<Response<EtsyAutoSyncResponse>>
    {
    }
}
