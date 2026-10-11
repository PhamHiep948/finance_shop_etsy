namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class AttachmentResponse
    {
        public long Id { get; set; }
        public long EntryId { get; set; }
        public string OriginalName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
