using Fin.Infrastructure.Finance.Models;

namespace Fin.Infrastructure.Finance.Interfaces
{
    public interface IEcbRateProvider
    {
        Task<EcbRate> GetLatestAsync(CancellationToken cancellationToken);

        Task<Dictionary<string, decimal>> GetMonthlyAveragesAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);
    }
}
