using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Commands
{
    public sealed class ImportEtsyOrdersCommand : IRequest<Response<EtsyImportResponse>>
    {
        public List<EtsyOrderDto> Orders { get; set; } = [];
    }
}
