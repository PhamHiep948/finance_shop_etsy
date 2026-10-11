namespace Fin.Model.Dto.Finance.Requests
{
    public sealed class CategoryGroupRequest
    {
        public string? Name { get; set; }
        public List<long>? CategoryIds { get; set; }
    }
}
