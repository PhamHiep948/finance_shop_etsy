using Fin.Domain.Finance.Entities;

namespace Fin.Infrastructure.Finance.Interfaces
{
    public interface IFxMonthlyRateRepository
    {
        Task<Dictionary<string, decimal>> GetAllAsync(CancellationToken cancellationToken);

        Task<FxMonthlyRate?> GetAsync(string month, CancellationToken cancellationToken);

        void Add(FxMonthlyRate rate);

        void Remove(FxMonthlyRate rate);
    }
}
