namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class PeriodLockResponse
    {
        public bool Enabled { get; set; }
        public string Period { get; set; } = string.Empty;
        public int GraceDays { get; set; }
        public string Today { get; set; } = string.Empty;
    }
}
