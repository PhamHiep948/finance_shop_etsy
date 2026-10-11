using Fin.UnitOfWork;

namespace Fin.Infrastructure.Finance.Interfaces
{
    public interface IFinanceUnitOfWork : IUnitOfWork
    {
        ICategoryRepository CategoryRepository { get; }

        ILedgerEntryRepository LedgerEntryRepository { get; }

        IAttachmentRepository AttachmentRepository { get; }

        IAuditLogRepository AuditLogRepository { get; }

        IFxMonthlyRateRepository FxMonthlyRateRepository { get; }

        ICategoryGroupRepository CategoryGroupRepository { get; }
    }
}
