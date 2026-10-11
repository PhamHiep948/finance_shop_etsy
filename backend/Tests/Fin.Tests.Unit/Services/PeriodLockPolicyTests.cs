using Fin.Infrastructure.Finance.Options;
using Fin.Service.Finance.Services;
using Fin.Tests.Unit.Support;

namespace Fin.Tests.Unit.Services
{
    public sealed class PeriodLockPolicyTests
    {
        private static PeriodLockPolicy Create(DateTime today, string period = "Quarter", int graceDays = 1, bool enabled = true) =>
            new(new StaticOptionsMonitor<PeriodLockOptions>(new PeriodLockOptions { Enabled = enabled, Period = period, GraceDays = graceDays }),
                new FixedTimeProvider(today));

        [Fact]
        public void Quarter_stays_open_until_the_grace_day_of_the_next_quarter()
        {
            var policy = Create(new DateTime(2026, 10, 1));

            Assert.False(policy.IsLocked(new DateOnly(2026, 9, 30)));
            Assert.Equal(new DateOnly(2026, 10, 1), policy.LastEditableDay(new DateOnly(2026, 7, 15)));
        }

        [Fact]
        public void Previous_quarter_is_locked_after_the_grace_day()
        {
            var policy = Create(new DateTime(2026, 10, 2));

            var message = policy.Check("2026-08-20");

            Assert.NotNull(message);
            Assert.StartsWith("Quý 3/2026 đã khóa sổ", message);
            Assert.Contains("01/10/2026", message);
        }

        [Fact]
        public void Monthly_period_locks_only_the_previous_month()
        {
            var policy = Create(new DateTime(2026, 10, 2), period: "Month");

            Assert.True(policy.IsLocked(new DateOnly(2026, 9, 30)));
            Assert.False(policy.IsLocked(new DateOnly(2026, 10, 1)));
            Assert.Equal("MONTH", policy.ToResponse().Period);
        }

        [Fact]
        public void Disabled_lock_never_blocks()
        {
            var policy = Create(new DateTime(2027, 6, 1), enabled: false);

            Assert.Null(policy.Check("2026-01-01"));
        }

        [Fact]
        public void Invalid_date_text_is_not_treated_as_locked()
        {
            Assert.Null(Create(new DateTime(2026, 10, 2)).Check("not-a-date"));
        }
    }
}
