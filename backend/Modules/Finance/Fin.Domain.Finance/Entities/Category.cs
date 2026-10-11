using Fin.Domain.Shared.Common;

namespace Fin.Domain.Finance.Entities
{
    public sealed class Category : BaseEntity
    {
        public string Kind { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}
