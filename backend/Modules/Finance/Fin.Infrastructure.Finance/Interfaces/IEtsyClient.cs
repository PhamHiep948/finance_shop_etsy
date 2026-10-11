using Fin.Infrastructure.Finance.Models;

namespace Fin.Infrastructure.Finance.Interfaces
{
    public interface IEtsyClient
    {
        bool IsReady { get; }

        Task<EtsyConnectionStatus> GetStatusAsync(CancellationToken cancellationToken);

        Task<string> CreateAuthorizeUrlAsync(CancellationToken cancellationToken);

        Task CompleteAuthorizationAsync(string code, string state, CancellationToken cancellationToken);

        void Disconnect();

        Task<IReadOnlyList<EtsyReceipt>> GetPaidReceiptsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken);
    }
}
