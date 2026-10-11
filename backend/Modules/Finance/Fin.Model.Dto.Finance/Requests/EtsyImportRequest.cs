using Fin.Model.Dto.Finance.Responses;

namespace Fin.Model.Dto.Finance.Requests
{
    public sealed class EtsyImportRequest
    {
        public List<EtsyOrderDto>? Orders { get; set; }
    }
}
