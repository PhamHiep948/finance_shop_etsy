using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Queries
{
    public sealed class GetEtsyOrdersQuery : IRequest<Response<List<EtsyOrderDto>>>
    {
        public string? From { get; set; }
        public string? To { get; set; }
    }
}
