using Fin.Domain.Shared.Common;

namespace Fin.Domain.Finance.Entities
{
    public sealed class Attachment : BaseEntity
    {
        public long EntryId { get; set; }
        public string OriginalName { get; set; } = string.Empty;
        public string StoredName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public DateTime CreatedAt { get; set; }

        public LedgerEntry? Entry { get; set; }
    }
}
