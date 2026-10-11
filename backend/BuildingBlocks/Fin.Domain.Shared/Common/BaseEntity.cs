namespace Fin.Domain.Shared.Common
{
    public abstract class BaseEntity : IEntity<long>
    {
        public long Id { get; set; }
    }
}
