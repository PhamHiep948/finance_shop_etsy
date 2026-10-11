using Fin.Infrastructure.Shared.ErrorCodes;
using Fin.Infrastructure.Shared.Wrappers;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;
using MediatR;

namespace Fin.Service.Finance.UseCases.Etsy.Commands.Handlers
{
    public sealed class ImportEtsyOrdersHandler : IRequestHandler<ImportEtsyOrdersCommand, Response<EtsyImportResponse>>
    {
        private readonly EtsyOrderImporter _importer;

        public ImportEtsyOrdersHandler(EtsyOrderImporter importer)
        {
            _importer = importer;
        }

        public async Task<Response<EtsyImportResponse>> Handle(ImportEtsyOrdersCommand request, CancellationToken cancellationToken)
        {
            if (request.Orders.Count == 0)
            {
                return Response<EtsyImportResponse>.Fail(ErrorCodeEnum.ETS_ERR_002);
            }
            return Response<EtsyImportResponse>.Ok(await _importer.ImportAsync(request.Orders, "Nhập từ Etsy", cancellationToken));
        }
    }
}
