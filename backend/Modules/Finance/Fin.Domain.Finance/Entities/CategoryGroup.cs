using Fin.Domain.Shared.Common;

namespace Fin.Domain.Finance.Entities
{
    public sealed class CategoryGroup : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public List<long> CategoryIds { get; set; } = [];
        public DateTime CreatedAt { get; set; }

        public void SetCategories(IEnumerable<long> ids) => CategoryIds = ids.Where(id => id > 0).Distinct().Order().ToList();
    }
}
