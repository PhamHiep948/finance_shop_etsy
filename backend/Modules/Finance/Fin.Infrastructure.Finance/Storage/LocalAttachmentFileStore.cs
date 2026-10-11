using Fin.Infrastructure.Finance.Interfaces;

namespace Fin.Infrastructure.Finance.Storage
{
    public sealed class LocalAttachmentFileStore : IAttachmentFileStore
    {
        private readonly FinanceStoragePaths _paths;

        public LocalAttachmentFileStore(FinanceStoragePaths paths)
        {
            _paths = paths;
        }

        public async Task<string> SaveAsync(Stream content, string extension, CancellationToken cancellationToken)
        {
            Directory.CreateDirectory(_paths.UploadsFolder);
            // A random stored name keeps user-supplied file names out of the file system path.
            var storedName = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
            await using var file = File.Create(GetPath(storedName));
            await content.CopyToAsync(file, cancellationToken);
            return storedName;
        }

        public string GetPath(string storedName) => Path.Combine(_paths.UploadsFolder, Path.GetFileName(storedName));

        public void Delete(string storedName)
        {
            var path = GetPath(storedName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
