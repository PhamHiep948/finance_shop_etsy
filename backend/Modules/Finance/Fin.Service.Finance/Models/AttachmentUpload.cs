namespace Fin.Service.Finance.Models
{
    public sealed record AttachmentUpload(string FileName, long Length, Func<Stream> OpenReadStream);
}
