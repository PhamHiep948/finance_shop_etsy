using Fin.Infrastructure.Finance.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Fin.Infrastructure.Finance.Storage
{
    public sealed class FinanceStoragePaths
    {
        public FinanceStoragePaths(IHostEnvironment environment, IOptions<FinanceStorageOptions> options)
        {
            var root = environment.ContentRootPath;
            UploadsFolder = Path.GetFullPath(Path.Combine(root, options.Value.UploadsFolder));
            EtsyTokenFile = Path.GetFullPath(Path.Combine(root, options.Value.EtsyTokenFile));
            EtsyAutoSyncFile = Path.GetFullPath(Path.Combine(root, options.Value.EtsyAutoSyncFile));
        }

        public string UploadsFolder { get; }
        public string EtsyTokenFile { get; }
        public string EtsyAutoSyncFile { get; }
    }
}
