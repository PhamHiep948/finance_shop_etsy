namespace Fin.Model.Dto.Finance.Requests
{
    public sealed class EtsyAutoSyncRequest
    {
        public bool Enabled { get; set; }
        public int IntervalMinutes { get; set; } = 60;
    }
}
