using Fin.Domain.Shared.Common;

namespace Fin.Domain.Finance.Entities
{
    public sealed class AuditLog : BaseEntity
    {
        public const int LabelMaxLength = 500;

        public string EntityType { get; set; } = string.Empty;
        public long EntityId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string? Detail { get; set; }
        public string? Changes { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
