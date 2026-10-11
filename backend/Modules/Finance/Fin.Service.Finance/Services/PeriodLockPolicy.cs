using System.Globalization;
using Fin.Infrastructure.Finance.Options;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Constants;
using Microsoft.Extensions.Options;

namespace Fin.Service.Finance.Services
{
    public sealed class PeriodLockPolicy
    {
        private readonly IOptionsMonitor<PeriodLockOptions> _options;
        private readonly TimeProvider _timeProvider;

        public PeriodLockPolicy(IOptionsMonitor<PeriodLockOptions> options, TimeProvider timeProvider)
        {
            _options = options;
            _timeProvider = timeProvider;
        }

        public DateOnly Today => DateOnly.FromDateTime(_timeProvider.GetLocalNow().DateTime);

        private PeriodLockOptions Settings => _options.CurrentValue;

        private bool Quarterly => !Settings.Period.Equals("Month", StringComparison.OrdinalIgnoreCase);

        public DateOnly LastEditableDay(DateOnly date) =>
            PeriodStart(date).AddMonths(Quarterly ? 3 : 1).AddDays(Math.Max(0, Settings.GraceDays) - 1);

        public bool IsLocked(DateOnly date) => Settings.Enabled && Today > LastEditableDay(date);

        public string? Check(string? isoDate) =>
            DateOnly.TryParseExact(isoDate, FinanceConstants.IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                ? Check(date)
                : null;

        public string? Check(DateOnly date)
        {
            if (!IsLocked(date))
            {
                return null;
            }

            var lastDay = LastEditableDay(date).ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
            return $"{PeriodName(date)} đã khóa sổ (chỉ được chỉnh sửa đến hết ngày {lastDay}). "
                + "Không thể thêm, sửa, xóa hay đổi chứng từ của khoản trong kỳ này.";
        }

        public PeriodLockResponse ToResponse() => new()
        {
            Enabled = Settings.Enabled,
            Period = Quarterly ? "QUARTER" : "MONTH",
            GraceDays = Math.Max(0, Settings.GraceDays),
            Today = Today.ToString(FinanceConstants.IsoDateFormat, CultureInfo.InvariantCulture),
        };

        private DateOnly PeriodStart(DateOnly date) => new(date.Year, Quarterly ? (date.Month - 1) / 3 * 3 + 1 : date.Month, 1);

        private string PeriodName(DateOnly date) =>
            Quarterly ? $"Quý {(date.Month - 1) / 3 + 1}/{date.Year}" : $"Tháng {date.Month}/{date.Year}";
    }
}
