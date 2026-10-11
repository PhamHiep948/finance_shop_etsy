using System.Globalization;
using Microsoft.Extensions.Options;

/// <summary>Cấu hình khóa sổ trong appsettings.json (mục "PeriodLock").</summary>
public sealed class PeriodLockOptions
{
    public bool Enabled { get; set; } = true;
    /// <summary>Kỳ khóa sổ: "Quarter" (theo quý, mặc định) hoặc "Month" (theo tháng).</summary>
    public string Period { get; set; } = "Quarter";
    /// <summary>Số ngày của kỳ sau vẫn cho sửa kỳ trước (1 = được sửa thêm ngày đầu tiên của kỳ sau).</summary>
    public int GraceDays { get; set; } = 1;
}

/// <summary>
/// Khóa sổ theo quý (hoặc theo tháng): hết kỳ thì không được thêm / sửa / xóa khoản của kỳ đó nữa.
/// Có GraceDays ngày "du di" sang kỳ sau để kịp chỉnh các khoản của ngày cuối kỳ.
/// </summary>
public sealed class PeriodLock(IOptionsMonitor<PeriodLockOptions> options)
{
    private PeriodLockOptions O => options.CurrentValue;
    private bool Quarterly => !O.Period.Equals("Month", StringComparison.OrdinalIgnoreCase);
    public static DateOnly Today => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Ngày đầu tiên của kỳ (quý / tháng) chứa `date`.</summary>
    private DateOnly PeriodStart(DateOnly date) =>
        new(date.Year, Quarterly ? (date.Month - 1) / 3 * 3 + 1 : date.Month, 1);

    /// <summary>Ngày cuối cùng còn được chỉnh sửa các khoản thuộc kỳ của `date`.</summary>
    public DateOnly LastEditableDay(DateOnly date) =>
        PeriodStart(date).AddMonths(Quarterly ? 3 : 1).AddDays(Math.Max(0, O.GraceDays) - 1);

    public bool IsLocked(DateOnly date) => O.Enabled && Today > LastEditableDay(date);

    private string PeriodName(DateOnly date) =>
        Quarterly ? $"Quý {(date.Month - 1) / 3 + 1}/{date.Year}" : $"Tháng {date.Month}/{date.Year}";

    /// <summary>Trả về thông báo lỗi nếu ngày (yyyy-MM-dd) thuộc kỳ đã khóa, ngược lại null.</summary>
    public string? Check(string? isoDate)
    {
        if (!DateOnly.TryParseExact(isoDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return null;
        if (!IsLocked(date)) return null;
        return $"{PeriodName(date)} đã khóa sổ (chỉ được chỉnh sửa đến hết ngày {LastEditableDay(date):dd/MM/yyyy}). " +
               "Không thể thêm, sửa, xóa hay đổi chứng từ của khoản trong kỳ này.";
    }

    public object Settings() => new
    {
        enabled = O.Enabled,
        period = Quarterly ? "QUARTER" : "MONTH",
        graceDays = Math.Max(0, O.GraceDays),
        today = Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
    };
}
