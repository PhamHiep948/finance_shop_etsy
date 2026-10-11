using Fin.Domain.Finance.Entities;

namespace Fin.Infrastructure.Finance.Interfaces
{
    public interface ICategoryGroupRepository
    {
        Task<List<CategoryGroup>> GetListAsync(CancellationToken cancellationToken);

        Task<CategoryGroup?> GetByIdAsync(long id, CancellationToken cancellationToken);

        Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken cancellationToken);

        void Add(CategoryGroup group);

        void Remove(CategoryGroup group);
    }
}
