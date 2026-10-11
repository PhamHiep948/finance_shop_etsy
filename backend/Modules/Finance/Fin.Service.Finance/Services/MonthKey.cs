using System.Globalization;
using Fin.Service.Finance.Constants;

namespace Fin.Service.Finance.Services
{
    public static class MonthKey
    {
        public static bool TryParse(string? month, out DateOnly firstDay) =>
            DateOnly.TryParseExact(month + "-01", FinanceConstants.IsoDateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out firstDay);

        public static string Of(DateOnly date) => date.ToString(FinanceConstants.MonthFormat, CultureInfo.InvariantCulture);
    }
}
