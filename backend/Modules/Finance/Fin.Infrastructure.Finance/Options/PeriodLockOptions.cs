namespace Fin.Infrastructure.Finance.Options
{
    public sealed class PeriodLockOptions
    {
        public const string SectionName = "PeriodLock";

        public bool Enabled { get; set; } = true;
        public string Period { get; set; } = "Quarter";
        public int GraceDays { get; set; } = 1;
    }
}
