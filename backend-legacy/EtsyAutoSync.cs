using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;

/// <summary>Nhập đơn Etsy vào sổ thành khoản thu (dùng chung cho nút "Nhập đơn" và đồng bộ tự động).</summary>
public static class EtsyImporter
{
    public const string Source = "ETSY"; // Khoản thu nhập từ Etsy: chỉ xem / xóa, không sửa.

    /// <summary>Nhập các đơn chưa có trong sổ; trả về (số đơn đã nhập, số đơn bỏ qua).</summary>
    public static async Task<(int Imported, int Skipped)> ImportAsync(
        IReadOnlyCollection<EtsyOrder> orders, LedgerStore store, AuditStore audit, EtsyOptions options, string auditNote)
    {
        if (orders.Count == 0) return (0, 0);
        var categoryId = options.DefaultCategoryId;
        if (!await store.CategoryExistsAsync(categoryId, "INCOME"))
            categoryId = await store.FirstCategoryIdAsync("INCOME");
        if (categoryId == 0) throw new EtsyException("Chưa có loại thu nào để gán cho đơn Etsy.");
        var existing = await store.ExistingOrderCodesAsync("ETSY_STORE", orders.Select(o => o.ReceiptId).ToList());
        var imported = 0;
        // Đơn Etsy là dữ liệu gốc (không sửa được, không nhập trùng) nên được nhập cả vào kỳ đã khóa sổ,
        // để lấy bù đơn của tháng / quý trước.
        foreach (var o in orders.Where(o => !existing.Contains(o.ReceiptId)).DistinctBy(o => o.ReceiptId))
        {
            if (!DateOnly.TryParseExact(o.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
                || string.IsNullOrWhiteSpace(o.Description) || o.Amount < 0) continue;
            var entry = new LedgerInput
            {
                Date = o.Date, Description = o.Description.Length > 500 ? o.Description[..500] : o.Description, CategoryId = categoryId,
                Amount = o.Amount, TaxPercent = o.TaxPercent, AmountAfterTax = o.AmountAfterTax, OrderCode = o.ReceiptId,
                SaleRegion = o.SaleRegion, SalesChannel = "ETSY_STORE", ProductQty = o.Quantity, PaymentMethod = "CREDIT_CARD",
                Extra = new Dictionary<string, JsonElement>
                {
                    ["itemTotal"] = JsonSerializer.SerializeToElement(o.ItemTotal),
                    ["discountAmount"] = JsonSerializer.SerializeToElement(o.Discount),
                    ["subtotal"] = JsonSerializer.SerializeToElement(o.Subtotal),
                    ["shippingAmount"] = JsonSerializer.SerializeToElement(o.Shipping),
                    ["taxAmount"] = JsonSerializer.SerializeToElement(o.Tax),
                    ["referenceCode"] = JsonSerializer.SerializeToElement($"ETSY-{o.ReceiptId}"),
                    ["note"] = JsonSerializer.SerializeToElement(string.Join(" ", new[] { $"Nhập từ Etsy ({o.Country}, {o.Currency}).", o.Note }.Where(x => !string.IsNullOrEmpty(x)))),
                },
            };
            var id = await store.InsertAsync("INCOME", entry, Source);
            await audit.LogAsync("INCOME", id, "CREATE", entry.Description, auditNote, AuditStore.Diff(null, await store.GetAsync("INCOME", id)));
            imported++;
        }
        return (imported, orders.Count - imported);
    }
}

/// <summary>Cài đặt + kết quả lần chạy gần nhất của đồng bộ tự động (lưu ở backend/etsy.autosync.json).</summary>
public sealed class EtsyAutoSyncState
{
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = 60;
    /// <summary>Mỗi lần chạy lấy đơn của bấy nhiêu ngày gần nhất (đơn đã nhập thì bỏ qua).</summary>
    public int LookbackDays { get; set; } = 14;
    public DateTime? LastRunAt { get; set; }
    public int LastImported { get; set; }
    public int TotalImported { get; set; }
    public string? LastError { get; set; }
}

/// <summary>
/// Đồng bộ đơn Etsy tự động: khi đã bật và đã kết nối Etsy, cứ mỗi IntervalMinutes lại lấy đơn của
/// LookbackDays ngày gần nhất và nhập các đơn mới thành khoản thu.
/// </summary>
public sealed class EtsyAutoSync(IServiceScopeFactory scopes, EtsyService etsy, IOptionsMonitor<EtsyOptions> options,
    IWebHostEnvironment env, ILogger<EtsyAutoSync> log) : BackgroundService
{
    public static readonly int[] Intervals = [15, 30, 60, 360, 1440];
    private readonly SemaphoreSlim runLock = new(1, 1);
    private readonly object fileLock = new();
    private EtsyAutoSyncState? state;
    private string FilePath => Path.Combine(env.ContentRootPath, "etsy.autosync.json");

    public bool Running => runLock.CurrentCount == 0;

    public EtsyAutoSyncState State
    {
        get
        {
            lock (fileLock)
            {
                if (state is not null) return state;
                try { state = File.Exists(FilePath) ? JsonSerializer.Deserialize<EtsyAutoSyncState>(File.ReadAllText(FilePath)) : null; }
                catch (Exception e) when (e is IOException or JsonException) { state = null; }
                return state ??= new EtsyAutoSyncState();
            }
        }
    }

    private void Save()
    {
        lock (fileLock) File.WriteAllText(FilePath, JsonSerializer.Serialize(State, new JsonSerializerOptions { WriteIndented = true }));
    }

    public object Status() => new
    {
        State.Enabled, State.IntervalMinutes, State.LookbackDays, State.LastRunAt, State.LastImported, State.TotalImported, State.LastError,
        Running, Ready = etsy.Ready,
        NextRunAt = State.Enabled && etsy.Ready ? (State.LastRunAt ?? DateTime.Now).AddMinutes(State.LastRunAt is null ? 0 : State.IntervalMinutes) : (DateTime?)null,
        Intervals,
    };

    public void Configure(bool enabled, int intervalMinutes)
    {
        State.Enabled = enabled;
        if (Intervals.Contains(intervalMinutes)) State.IntervalMinutes = intervalMinutes;
        Save();
    }

    /// <summary>Chạy một lần (bỏ qua nếu đang có lần chạy khác). Lỗi được ghi vào LastError, không ném ra ngoài.</summary>
    public async Task RunOnceAsync(CancellationToken ct = default)
    {
        if (!await runLock.WaitAsync(0, ct)) return;
        try
        {
            var to = DateOnly.FromDateTime(DateTime.Now);
            var from = to.AddDays(-Math.Clamp(State.LookbackDays, 1, 90));
            using var scope = scopes.CreateScope();
            var sp = scope.ServiceProvider;
            var orders = await etsy.FetchOrdersAsync(from, to, await sp.GetRequiredService<FxMonthlyStore>().AllAsync());
            var (imported, _) = await EtsyImporter.ImportAsync(orders, sp.GetRequiredService<LedgerStore>(), sp.GetRequiredService<AuditStore>(),
                options.CurrentValue, "Tự động đồng bộ từ Etsy");
            State.LastImported = imported;
            State.TotalImported += imported;
            State.LastError = null;
            if (imported > 0) log.LogInformation("Đồng bộ Etsy tự động: nhập {Count} đơn mới", imported);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            State.LastImported = 0;
            State.LastError = e is EtsyException or HttpRequestException ? e.Message : "Lỗi khi đồng bộ: " + e.Message;
            log.LogWarning(e, "Đồng bộ Etsy tự động lỗi");
        }
        finally
        {
            State.LastRunAt = DateTime.Now;
            Save();
            runLock.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Kiểm tra mỗi phút xem đã đến lượt chạy chưa (đổi chu kỳ / bật tắt có hiệu lực ngay).
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            var s = State;
            if (s.Enabled && etsy.Ready && (s.LastRunAt is null || DateTime.Now - s.LastRunAt >= TimeSpan.FromMinutes(s.IntervalMinutes)))
                await RunOnceAsync(stoppingToken);
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
