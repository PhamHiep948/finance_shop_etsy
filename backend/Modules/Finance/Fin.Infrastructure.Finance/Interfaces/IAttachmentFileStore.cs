namespace Fin.Infrastructure.Finance.Interfaces
{
    public interface IAttachmentFileStore
    {
        Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken);

        string GetPath(string storedName);

        void Delete(string storedName);
    }
}
