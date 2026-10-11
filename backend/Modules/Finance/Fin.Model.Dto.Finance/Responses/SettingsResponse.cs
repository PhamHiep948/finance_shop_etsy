namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class SettingsResponse
    {
        public PeriodLockResponse PeriodLock { get; set; } = new();
    }
}
