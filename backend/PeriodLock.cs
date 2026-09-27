using System.Globalization;
using Microsoft.Extensions.Options;

/// <summary>Cấu hình khóa sổ trong appsettings.json (mục "PeriodLock").</summary>
public sealed class PeriodLockOptions
{
    public bool Enabled { get; set; } = true;
    /// <summary>Số ngày của tháng sau vẫn cho sửa tháng trước (1 = được sửa thêm ngày mùng 1).</summary>
    public int GraceDays { get; set; } = 1;
}

/// <summary>
/// Khóa sổ theo tháng: hết tháng thì không được thêm / sửa / xóa khoản của tháng đó nữa.
/// Có GraceDays ngày "du di" sang tháng sau để kịp chỉnh các khoản của ngày cuối tháng.
/// </summary>
public sealed class PeriodLock(IOptionsMonitor<PeriodLockOptions> options)
{
    private PeriodLockOptions O => options.CurrentValue;
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Ngày cuối cùng còn được chỉnh sửa các khoản thuộc tháng của `date`.</summary>
    public DateOnly LastEditableDay(DateOnly date) =>
        new DateOnly(date.Year, date.Month, 1).AddMonths(1).AddDays(Math.Max(0, O.GraceDays) - 1);

    public bool IsLocked(DateOnly date) => O.Enabled && Today > LastEditableDay(date);

    /// <summary>Trả về thông báo lỗi nếu ngày (yyyy-MM-dd) thuộc tháng đã khóa, ngược lại null.</summary>
    public string? Check(string? isoDate)
    {
        if (!DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return null;
        if (!IsLocked(date)) return null;
        return $"Tháng {date.Month}/{date.Year} đã khóa sổ (chỉ được chỉnh sửa đến hết ngày {LastEditableDay(date):dd/MM/yyyy}). " +
               "Không thể thêm, sửa, xóa hay đổi chứng từ của khoản trong tháng này.";
    }

    public object Settings() => new
    {
        enabled = O.Enabled,
        graceDays = Math.Max(0, O.GraceDays),
        today = Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    };
}
