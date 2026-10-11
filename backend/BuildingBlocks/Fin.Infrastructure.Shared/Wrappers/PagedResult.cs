namespace Fin.Infrastructure.Shared.Wrappers
{
    public sealed class PagedResult<T>
    {
        public PagedResult(IReadOnlyList<T> items, int page, int pageSize, long totalItems)
        {
            Items = items;
            Page = page;
            PageSize = pageSize;
            TotalItems = totalItems;
        }

        public IReadOnlyList<T> Items { get; }
        public int Page { get; }
        public int PageSize { get; }
        public long TotalItems { get; }
        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);
    }
}
