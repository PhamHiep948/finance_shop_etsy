using Fin.Infrastructure.Finance.Models;

namespace Fin.Infrastructure.Finance.Interfaces
{
    public interface IEtsyAutoSyncStateStore
    {
        EtsyAutoSyncState Load();

        void Save(EtsyAutoSyncState state);
    }
}
