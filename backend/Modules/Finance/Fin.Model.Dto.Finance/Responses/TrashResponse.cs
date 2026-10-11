namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class TrashResponse
    {
        public int RetentionDays { get; set; }
        public IReadOnlyList<LedgerEntryResponse> Items { get; set; } = [];
    }
}
