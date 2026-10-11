using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Contexts;
using Fin.Infrastructure.Finance.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Finance.Repositories
{
    public sealed class FxMonthlyRateRepository : IFxMonthlyRateRepository
    {
        private readonly FinanceContext _dbContext;

        public FxMonthlyRateRepository(FinanceContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Dictionary<string, decimal>> GetAllAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.FxMonthlyRates.AsNoTracking()
                .OrderByDescending(x => x.Month)
                .ToDictionaryAsync(x => x.Month, x => x.EurToUsd, cancellationToken);
        }

        public async Task<FxMonthlyRate?> GetAsync(string month, CancellationToken cancellationToken)
        {
            return await _dbContext.FxMonthlyRates.FirstOrDefaultAsync(x => x.Month == month, cancellationToken);
        }

        public void Add(FxMonthlyRate rate) => _dbContext.FxMonthlyRates.Add(rate);

        public void Remove(FxMonthlyRate rate) => _dbContext.FxMonthlyRates.Remove(rate);
    }
}
