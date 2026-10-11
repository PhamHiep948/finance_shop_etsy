namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class AttachmentFileResponse
    {
        public string Path { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public bool CanPreview { get; set; }
    }
}
