using System.Text.Json;

namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class AuditLogResponse
    {
        public long Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public long EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public JsonElement? Changes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
