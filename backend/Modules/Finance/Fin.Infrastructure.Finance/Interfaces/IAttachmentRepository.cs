using Fin.Domain.Finance.Entities;
using Fin.Model.Dto.Finance.Responses;

namespace Fin.Infrastructure.Finance.Interfaces
{
    public interface IAttachmentRepository
    {
        Task<List<AttachmentResponse>> GetByEntryAsync(long entryId, CancellationToken cancellationToken);

        Task<Attachment?> GetByIdAsync(long id, CancellationToken cancellationToken);

        Task<List<string>> GetStoredNamesAsync(IReadOnlyCollection<long> entryIds, CancellationToken cancellationToken);

        void Add(Attachment attachment);

        void Remove(Attachment attachment);
    }
}
