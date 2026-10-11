using Fin.Domain.Finance.Constants;
using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Service.Finance.Constants;

namespace Fin.Service.Finance.Services
{
    public sealed class TrashPurger
    {
        private readonly IFinanceUnitOfWork _unitOfWork;
        private readonly IAttachmentFileStore _fileStore;
        private readonly AuditTrail _auditTrail;
        private readonly TimeProvider _timeProvider;

        public TrashPurger(IFinanceUnitOfWork unitOfWork, IAttachmentFileStore fileStore, AuditTrail auditTrail, TimeProvider timeProvider)
        {
            _unitOfWork = unitOfWork;
            _fileStore = fileStore;
            _auditTrail = auditTrail;
            _timeProvider = timeProvider;
        }

        public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken)
        {
            var cutoff = _timeProvider.GetLocalNow().DateTime.AddDays(-FinanceConstants.TrashRetentionDays);
            var expired = await _unitOfWork.LedgerEntryRepository.GetDeletedAsync(cutoff, cancellationToken);
            return await PurgeAsync(expired, $"Tự xóa sau {FinanceConstants.TrashRetentionDays} ngày trong thùng rác", cancellationToken);
        }

        public async Task<int> PurgeAsync(IReadOnlyList<LedgerEntry> entries, string reason, CancellationToken cancellationToken)
        {
            if (entries.Count == 0)
            {
                return 0;
            }

            var storedNames = await _unitOfWork.AttachmentRepository.GetStoredNamesAsync(entries.Select(e => e.Id).ToList(), cancellationToken);
            foreach (var entry in entries)
            {
                // Attachment rows go with the entry through ON DELETE CASCADE; only the files need explicit cleanup.
                _unitOfWork.LedgerEntryRepository.Remove(entry);
                _auditTrail.Log(entry.Kind, entry.Id, AuditActions.Purge, entry.Description, reason);
            }
            await _unitOfWork.CommitAsync(cancellationToken);

            // Files are removed only after the rows are gone, so a failed commit never leaves rows without files.
            foreach (var storedName in storedNames)
            {
                _fileStore.Delete(storedName);
            }
            return entries.Count;
        }
    }
}
