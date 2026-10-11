using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Contexts;
using Fin.Infrastructure.Finance.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Finance.Repositories
{
    public sealed class CategoryGroupRepository : ICategoryGroupRepository
    {
        private readonly FinanceContext _dbContext;

        public CategoryGroupRepository(FinanceContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<CategoryGroup>> GetListAsync(CancellationToken cancellationToken)
        {
            return await _dbContext.CategoryGroups.AsNoTracking().OrderBy(x => x.Id).ToListAsync(cancellationToken);
        }

        public async Task<CategoryGroup?> GetByIdAsync(long id, CancellationToken cancellationToken)
        {
            return await _dbContext.CategoryGroups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        }

        public async Task<bool> NameExistsAsync(string name, long? excludeId, CancellationToken cancellationToken)
        {
            return await _dbContext.CategoryGroups.AnyAsync(x => x.Name == name && (excludeId == null || x.Id != excludeId), cancellationToken);
        }

        public void Add(CategoryGroup group) => _dbContext.CategoryGroups.Add(group);

        public void Remove(CategoryGroup group) => _dbContext.CategoryGroups.Remove(group);
    }
}
