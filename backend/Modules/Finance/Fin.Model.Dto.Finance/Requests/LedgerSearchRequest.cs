namespace Fin.Model.Dto.Finance.Requests
{
    public sealed class LedgerSearchRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public string? Search { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }
}
