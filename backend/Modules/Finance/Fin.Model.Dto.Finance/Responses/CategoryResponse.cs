namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class CategoryResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public long Used { get; set; }
    }
}
