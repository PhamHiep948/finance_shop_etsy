using System.Text.Json;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Finance.Models;

namespace Fin.Infrastructure.Finance.Storage
{
    public sealed class FileEtsyAutoSyncStateStore : IEtsyAutoSyncStateStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly FinanceStoragePaths _paths;
        private readonly Lock _gate = new();

        public FileEtsyAutoSyncStateStore(FinanceStoragePaths paths)
        {
            _paths = paths;
        }

        public EtsyAutoSyncState Load()
        {
            lock (_gate)
            {
                try
                {
                    return File.Exists(_paths.EtsyAutoSyncFile)
                        ? JsonSerializer.Deserialize<EtsyAutoSyncState>(File.ReadAllText(_paths.EtsyAutoSyncFile)) ?? new EtsyAutoSyncState()
                        : new EtsyAutoSyncState();
                }
                catch (Exception e) when (e is IOException or JsonException)
                {
                    // A damaged settings file falls back to "auto sync off" instead of crashing the background worker.
                    return new EtsyAutoSyncState();
                }
            }
        }

        public void Save(EtsyAutoSyncState state)
        {
            lock (_gate)
            {
                File.WriteAllText(_paths.EtsyAutoSyncFile, JsonSerializer.Serialize(state, JsonOptions));
            }
        }
    }
}
