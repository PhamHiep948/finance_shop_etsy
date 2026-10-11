namespace Fin.Model.Dto.Finance.Requests
{
    public sealed class AuditLogSearchRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 30;
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public long? EntityId { get; set; }
        public string? Search { get; set; }
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }
}
