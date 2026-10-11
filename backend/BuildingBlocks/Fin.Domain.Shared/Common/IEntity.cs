namespace Fin.Domain.Shared.Common
{
    public interface IEntity<TKey>
    {
        TKey Id { get; set; }
    }
}
