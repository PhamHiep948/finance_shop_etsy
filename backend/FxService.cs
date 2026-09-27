using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

/// <summary>
/// Tỷ giá EUR → USD lấy tự động từ Frankfurter (tỷ giá tham chiếu của Ngân hàng Trung ương châu Âu, miễn phí, không cần key).
/// Không lấy được (mất mạng...) thì dùng tỷ giá dự phòng "EurToUsd" trong etsy.settings.json.
/// </summary>
public sealed class FxService(IHttpClientFactory http, IOptionsMonitor<EtsyOptions> options, ILogger<FxService> log)
{
    private const string ApiBase = "https://api.frankfurter.dev/v1";
    private static readonly TimeSpan LatestTtl = TimeSpan.FromHours(6);
    // Tỷ giá của ngày đã qua không đổi nữa nên giữ luôn trong bộ nhớ; "covered" = các ngày đã hỏi Frankfurter.
    private readonly Dictionary<DateOnly, decimal> daily = [];
    private readonly HashSet<DateOnly> covered = [];
    private readonly SemaphoreSlim fetchLock = new(1, 1);
    private (decimal Rate, string Date, DateTime FetchedAt)? latest;

    public sealed record FxRate(decimal EurToUsd, string Date, bool Auto);

    /// <summary>Tỷ giá mới nhất (dùng để hiển thị số tiền theo EUR).</summary>
    public async Task<FxRate> LatestAsync()
    {
        await fetchLock.WaitAsync();
        try
        {
            if (latest is { } c && DateTime.UtcNow - c.FetchedAt < LatestTtl) return new(c.Rate, c.Date, true);
            try
            {
                using var doc = await GetAsync($"{ApiBase}/latest?base=EUR&symbols=USD");
                var rate = doc.RootElement.GetProperty("rates").GetProperty("USD").GetDecimal();
                var date = doc.RootElement.GetProperty("date").GetString() ?? "";
                latest = (rate, date, DateTime.UtcNow);
                return new(rate, date, true);
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
            {
                log.LogWarning(e, "Không lấy được tỷ giá EUR/USD mới nhất.");
                // Lần trước đã lấy được thì dùng tạm, chưa từng lấy được thì dùng tỷ giá dự phòng.
                return latest is { } old ? new(old.Rate, old.Date, true) : new(options.CurrentValue.EurToUsd, "", false);
            }
        }
        finally
        {
            fetchLock.Release();
        }
    }

    /// <summary>
    /// Tỷ giá EUR → USD cho từng ngày trong khoảng. Cuối tuần / ngày lễ không có tỷ giá thì lấy của ngày làm việc gần nhất trước đó.
    /// </summary>
    public async Task<Func<DateOnly, FxRate>> DailyAsync(DateOnly from, DateOnly to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        if (to > today) to = today;
        // Lùi thêm vài ngày để ngày đầu khoảng rơi vào cuối tuần / lễ vẫn có tỷ giá trước đó.
        var start = from.AddDays(-7);
        await fetchLock.WaitAsync();
        try
        {
            bool missing;
            lock (daily) missing = Enumerable.Range(0, Math.Max(0, to.DayNumber - start.DayNumber + 1)).Any(i => !covered.Contains(start.AddDays(i)));
            if (missing)
            {
                try
                {
                    using var doc = await GetAsync($"{ApiBase}/{Iso(start)}..{Iso(to)}?base=EUR&symbols=USD");
                    lock (daily)
                    {
                        foreach (var day in doc.RootElement.GetProperty("rates").EnumerateObject())
                            daily[DateOnly.ParseExact(day.Name, "yyyy-MM-dd", CultureInfo.InvariantCulture)] = day.Value.GetProperty("USD").GetDecimal();
                        // Hôm nay có thể chưa công bố tỷ giá nên không đánh dấu, lần sau hỏi lại.
                        for (var d = start; d < today && d <= to; d = d.AddDays(1)) covered.Add(d);
                    }
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
                {
                    log.LogWarning(e, "Không lấy được tỷ giá EUR/USD theo ngày.");
                }
            }
        }
        finally
        {
            fetchLock.Release();
        }
        var fallback = await LatestAsync();
        return date => RateOn(date) is { } found ? new(found.Rate, Iso(found.Date), true) : fallback;
    }

    private (decimal Rate, DateOnly Date)? RateOn(DateOnly date)
    {
        lock (daily)
        {
            for (var d = date; d >= date.AddDays(-10); d = d.AddDays(-1))
                if (daily.TryGetValue(d, out var rate)) return (rate, d);
        }
        return null;
    }

    private async Task<JsonDocument> GetAsync(string url)
    {
        using var client = http.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        using var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();
        return JsonDocument.Parse(await res.Content.ReadAsStringAsync());
    }

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
