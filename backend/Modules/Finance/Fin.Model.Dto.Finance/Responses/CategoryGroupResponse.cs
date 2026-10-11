namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class CategoryGroupResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public IReadOnlyList<long> CategoryIds { get; set; } = [];
    }
}
