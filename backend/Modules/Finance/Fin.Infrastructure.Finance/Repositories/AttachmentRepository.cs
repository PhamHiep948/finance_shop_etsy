using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Contexts;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Model.Dto.Finance.Responses;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Finance.Repositories
{
    public sealed class AttachmentRepository : IAttachmentRepository
    {
        private readonly FinanceContext _dbContext;

        public AttachmentRepository(FinanceContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<AttachmentResponse>> GetByEntryAsync(long entryId, CancellationToken cancellationToken)
        {
            return await _dbContext.Attachments.AsNoTracking()
                .Where(x => x.EntryId == entryId)
                .OrderBy(x => x.Id)
                .Select(x => new AttachmentResponse
                {
                    Id = x.Id,
                    EntryId = x.EntryId,
                    OriginalName = x.OriginalName,
                    ContentType = x.ContentType,
                    Size = x.SizeBytes,
                    CreatedAt = x.CreatedAt,
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<Attachment?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            return await _dbContext.Attachments.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<List<string>> GetStoredNamesAsync(IReadOnlyCollection<long> entryIds, CancellationToken cancellationToken)
        {
            if (entryIds.Count == 0)
            {
                return [];
            }

            return await _dbContext.Attachments.AsNoTracking()
                .Where(x => entryIds.Contains(x.EntryId))
                .Select(x => x.StoredName)
                .ToListAsync(cancellationToken);
        }

        public void Add(Attachment attachment) => _dbContext.Attachments.Add(attachment);

        public void Remove(Attachment attachment) => _dbContext.Attachments.Remove(attachment);
    }
}
