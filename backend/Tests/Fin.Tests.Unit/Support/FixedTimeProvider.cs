namespace Fin.Tests.Unit.Support
{
    public sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTime localNow)
        {
            _now = new DateTimeOffset(localNow, TimeSpan.Zero);
        }

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
