using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using MySqlConnector;

const int TrashRetentionDays = 30;
const string EtsySource = "ETSY"; // Đánh dấu khoản thu nhập từ Etsy: chỉ xem / xóa, không sửa.
const string LinkedSource = "LINKED"; // Khoản chi tiền xưởng / tiền ship tạo từ khoản thu (sửa được như khoản chi thường).

var builder = WebApplication.CreateBuilder(args);
// Key Etsy nằm trong file riêng (không đưa lên git). Sửa file này là có hiệu lực ngay, không cần chạy lại.
builder.Configuration.AddJsonFile("etsy.settings.json", optional: true, reloadOnChange: true);
builder.Services.Configure<EtsyOptions>(builder.Configuration.GetSection("Etsy"));
builder.Services.Configure<PeriodLockOptions>(builder.Configuration.GetSection("PeriodLock"));
builder.Services.AddSingleton<PeriodLock>();
builder.Services.AddCors(options => options.AddPolicy("FrontendLocal", policy =>
    policy.WithOrigins(
            "http://localhost:5173", "http://127.0.0.1:5173",
            "http://localhost:5500", "http://127.0.0.1:5500")
        .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddHttpClient();
builder.Services.AddScoped<LedgerStore>();
builder.Services.AddScoped<AttachmentStore>();
builder.Services.AddScoped<AuditStore>();
builder.Services.AddScoped<FxMonthlyStore>();
builder.Services.AddScoped<CategoryGroupStore>();
builder.Services.AddSingleton<FxService>();
builder.Services.AddSingleton<EtsyService>();
builder.Services.AddSingleton<EtsyAutoSync>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<EtsyAutoSync>());
var app = builder.Build();
app.UseCors("FrontendLocal");
// Phục vụ giao diện HTML/CSS/JS trong thư mục ../frontend tại http://localhost:5000/
var frontendDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "frontend"));
if (Directory.Exists(frontendDir))
{
    var frontendFiles = new PhysicalFileProvider(frontendDir);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendFiles });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = frontendFiles,
        // Trình duyệt luôn hỏi lại server (file không đổi thì trả 304, rất nhanh) → sửa JS/CSS xong chỉ cần tải lại trang.
        OnPrepareResponse = ctx => ctx.Context.Response.Headers.CacheControl = "no-cache",
    });
}
app.Use(async (context, next) =>
{
    try { await next(); }
    catch (MySqlException error)
    {
        app.Logger.LogError(error, "MySQL connection/query failed");
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { title = "Không kết nối được MySQL. Kiểm tra XAMPP, chuỗi kết nối và file SQL." });
    }
    catch (EtsyException error)
    {
        context.Response.StatusCode = 400;
        await context.Response.WriteAsJsonAsync(new { title = error.Message });
    }
});
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/api/v1/settings", (PeriodLock periodLock) => Results.Ok(new { periodLock = periodLock.Settings() }));

// ---------------------------------------------------------------- Loại thu / loại chi
app.MapGet("/api/v1/categories/{type}", async (string type, LedgerStore store) =>
{
    var kind = Kind(type);
    return kind is null ? Results.NotFound() : Results.Ok(await store.CategoriesAsync(kind));
});
app.MapPost("/api/v1/categories/{type}", async (string type, CategoryInput input, LedgerStore store, AuditStore audit) =>
{
    var kind = Kind(type);
    if (kind is null) return Results.NotFound();
    var name = input.Name?.Trim() ?? "";
    if (name.Length is 0 or > 150) return Results.BadRequest(new { title = "Tên loại phải có từ 1 đến 150 ký tự." });
    var id = await store.InsertCategoryAsync(kind, name);
    if (id is null) return Results.Conflict(new { title = $"Loại \"{name}\" đã tồn tại." });
    await audit.LogAsync("CATEGORY", id.Value, "CREATE", name, KindLabel(kind));
    return Results.Created($"/api/v1/categories/{type}/{id}", new { id, name, used = 0 });
});
app.MapPut("/api/v1/categories/{type}/{id:long}", async (string type, long id, CategoryInput input, LedgerStore store, AuditStore audit) =>
{
    var kind = Kind(type);
    if (kind is null || !await store.CategoryExistsAsync(id, kind)) return Results.NotFound();
    var name = input.Name?.Trim() ?? "";
    if (name.Length is 0 or > 150) return Results.BadRequest(new { title = "Tên loại phải có từ 1 đến 150 ký tự." });
    var oldName = await store.CategoryNameAsync(id);
    if (!await store.RenameCategoryAsync(id, name)) return Results.Conflict(new { title = $"Loại \"{name}\" đã tồn tại." });
    if (oldName != name)
        await audit.LogAsync("CATEGORY", id, "UPDATE", name, KindLabel(kind),
            new Dictionary<string, object?> { ["name"] = new { from = oldName, to = name } });
    return Results.Ok(new { id, name });
});
app.MapDelete("/api/v1/categories/{type}/{id:long}", async (string type, long id, LedgerStore store, AuditStore audit) =>
{
    var kind = Kind(type);
    if (kind is null || !await store.CategoryExistsAsync(id, kind)) return Results.NotFound();
    // Phạm vi khoản chi xóa tự do: các khoản đang dùng (kể cả trong thùng rác) chuyển về để trống phạm vi.
    if (kind == "SCOPE")
    {
        var scopeName = await store.CategoryNameAsync(id) ?? "";
        var cleared = await store.ClearScopeAsync(id);
        await store.DeleteCategoryAsync(id);
        await audit.LogAsync("CATEGORY", id, "DELETE", scopeName, cleared > 0 ? $"{KindLabel(kind)} · {cleared} khoản chuyển về để trống" : KindLabel(kind));
        return Results.NoContent();
    }
    var used = await store.CategoryUsageAsync(id, kind);
    if (used > 0)
        return Results.Conflict(new { title = $"Loại này đang được dùng bởi {used} khoản. Hãy chuyển các khoản đó sang loại khác trước khi xóa." });
    var inTrash = await store.CategoryTrashCountAsync(id, kind);
    if (inTrash > 0)
        return Results.Conflict(new { title = $"Thùng rác còn {inTrash} khoản thuộc loại này. Hãy khôi phục hoặc xóa vĩnh viễn chúng trước." });
    var name = await store.CategoryNameAsync(id) ?? "";
    await store.DeleteCategoryAsync(id);
    await audit.LogAsync("CATEGORY", id, "DELETE", name, KindLabel(kind));
    return Results.NoContent();
});

// ---------------------------------------------------------------- Nhóm loại (chọn nhanh nhiều loại trong báo cáo)
app.MapGet("/api/v1/category-groups", async (CategoryGroupStore groups) => Results.Ok(await groups.AllAsync()));
app.MapPost("/api/v1/category-groups", async (CategoryGroupInput input, CategoryGroupStore groups) =>
{
    var name = input.Name?.Trim() ?? "";
    if (name.Length is 0 or > 150) return Results.BadRequest(new { title = "Tên nhóm phải có từ 1 đến 150 ký tự." });
    var id = await groups.InsertAsync(name, input.CategoryIds ?? []);
    return id is null ? Results.Conflict(new { title = $"Nhóm \"{name}\" đã tồn tại." }) : Results.Ok(new { id });
});
app.MapPut("/api/v1/category-groups/{id:long}", async (long id, CategoryGroupInput input, CategoryGroupStore groups) =>
{
    if (!await groups.ExistsAsync(id)) return Results.NotFound();
    var name = input.Name?.Trim();
    if (name is { Length: 0 or > 150 }) return Results.BadRequest(new { title = "Tên nhóm phải có từ 1 đến 150 ký tự." });
    return await groups.UpdateAsync(id, name, input.CategoryIds)
        ? Results.NoContent() : Results.Conflict(new { title = $"Nhóm \"{name}\" đã tồn tại." });
});
app.MapDelete("/api/v1/category-groups/{id:long}", async (long id, CategoryGroupStore groups) =>
    await groups.DeleteAsync(id) ? Results.NoContent() : Results.NotFound());

// ---------------------------------------------------------------- Tiền xưởng / tiền ship của khoản thu
// Lưu thành một khoản chi gắn với khoản thu (details.linkedIncomeId), nên trang Khoản chi và báo cáo đều thấy số tiền này.
// Áp dụng cho mọi khoản thu, kể cả đơn nhập từ Etsy. Amount = số tiền đã trả (gồm thuế), USD; 0 = bỏ khoản chi đã gắn.
app.MapPut("/api/v1/incomes/{id:long}/cost", async (long id, IncomeCostInput input, LedgerStore store, AuditStore audit, PeriodLock periodLock) =>
{
    var income = await store.GetAsync("INCOME", id);
    if (income is null) return Results.NotFound();
    if (input.Amount < 0 || input.TaxPercent is < 0 or > 100) return Results.BadRequest(new { title = "Kiểm tra lại số tiền và % thuế." });
    var linkedId = (await store.LinkedExpenseIdsAsync(id)).FirstOrDefault();
    var existing = linkedId > 0 ? await store.GetAsync("EXPENSE", linkedId) : null;
    if (existing is not null && periodLock.Check(existing["date"] as string) is { } lockedOld)
        return Results.Conflict(new { title = lockedOld });
    if (input.Amount == 0)
    {
        if (existing is null) return Results.NoContent();
        await store.DeleteAsync("EXPENSE", linkedId);
        await audit.LogAsync("EXPENSE", linkedId, "DELETE", (string)existing["description"]!, "Bỏ tiền xưởng / tiền ship khỏi khoản thu");
        return Results.NoContent();
    }
    var date = DateOnly.TryParseExact(input.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)
        ? input.Date! : existing?["date"] as string ?? (string)income["date"]!;
    if (periodLock.Check(date) is { } locked) return Results.Conflict(new { title = locked });
    var inEu = income["saleRegion"] as string == "IN_EU";
    var categoryId = existing is not null ? (long)existing["categoryId"]! : await store.CategoryIdByNameAsync("EXPENSE", inEu ? "Phí ship" : "Tiền xưởng");
    var description = existing?["description"] as string ?? $"{(inEu ? "Tiền ship" : "Tiền xưởng")} · {income["description"]}";
    if (description.Length > 500) description = description[..500];
    var entry = new LedgerInput
    {
        Date = date, Description = description, CategoryId = categoryId,
        Amount = decimal.Round(input.Amount / (1 + input.TaxPercent / 100), 2, MidpointRounding.AwayFromZero),
        TaxPercent = input.TaxPercent, AmountAfterTax = input.Amount,
        OrderCode = income["orderCode"] as string, Payee = existing?["payee"] as string,
        OriginScope = existing?["originScope"] as string, PaymentMethod = existing?["paymentMethod"] as string,
        Extra = new Dictionary<string, JsonElement>
        {
            ["linkedIncomeId"] = JsonSerializer.SerializeToElement(id.ToString(CultureInfo.InvariantCulture)),
            ["invoiceId"] = JsonSerializer.SerializeToElement(input.InvoiceId?.Trim() ?? ""),
        },
    };
    if (existing is not null && existing.TryGetValue("note", out var note) && note is JsonElement noteValue) entry.Extra["note"] = noteValue;
    if (existing is null)
    {
        var newId = await store.InsertAsync("EXPENSE", entry, LinkedSource);
        await audit.LogAsync("EXPENSE", newId, "CREATE", description, $"Tiền xưởng / tiền ship của khoản thu #{id}",
            AuditStore.Diff(null, await store.GetAsync("EXPENSE", newId)));
        return Results.Ok(await store.GetAsync("EXPENSE", newId));
    }
    await store.UpdateAsync("EXPENSE", linkedId, entry, keepAfterTax: true);
    var after = await store.GetAsync("EXPENSE", linkedId);
    var changes = AuditStore.Diff(existing, after);
    if (changes.Count > 0) await audit.LogAsync("EXPENSE", linkedId, "UPDATE", description, $"Tiền xưởng / tiền ship của khoản thu #{id}", changes);
    return Results.Ok(after);
});

// ---------------------------------------------------------------- Chứng từ
app.MapGet("/api/v1/attachments/{id:long}", async (long id, HttpRequest req, AttachmentStore files) =>
{
    var file = await files.GetAsync(id);
    if (file is null || !File.Exists(file.Path)) return Results.NotFound();
    req.HttpContext.Response.Headers["X-Content-Type-Options"] = "nosniff";
    var inline = req.Query["download"] != "1" && AttachmentStore.CanPreview(file.ContentType);
    return inline
        ? Results.File(file.Path, file.ContentType)
        : Results.File(file.Path, file.ContentType, file.OriginalName);
});
app.MapDelete("/api/v1/attachments/{id:long}", async (long id, AttachmentStore files, LedgerStore store, AuditStore audit, PeriodLock periodLock) =>
{
    var file = await files.GetAsync(id);
    if (file is null) return Results.NotFound();
    var entry = await store.GetAnyAsync(file.EntryId);
    if (periodLock.Check(entry?["date"] as string) is { } locked) return Results.Conflict(new { title = locked });
    if (!await files.DeleteAsync(id)) return Results.NotFound();
    if (entry is not null)
        await audit.LogAsync((string)entry["kind"]!, file.EntryId, "ATTACH_DELETE", (string)entry["description"]!, file.OriginalName);
    return Results.NoContent();
});

// ---------------------------------------------------------------- Thùng rác
app.MapGet("/api/v1/trash", async (LedgerStore store, AttachmentStore files, AuditStore audit) =>
{
    await PurgeExpiredAsync(store, files, audit);
    return Results.Ok(new { retentionDays = TrashRetentionDays, items = await store.ListDeletedAsync() });
});
app.MapPost("/api/v1/trash/{id:long}/restore", async (long id, LedgerStore store, AuditStore audit, PeriodLock periodLock) =>
{
    var row = await store.GetAnyAsync(id);
    if (row is null || row["deletedAt"] is null) return Results.NotFound();
    if (periodLock.Check(row["date"] as string) is { } locked) return Results.Conflict(new { title = locked });
    if (!await store.RestoreAsync(id)) return Results.NotFound();
    await audit.LogAsync((string)row["kind"]!, id, "RESTORE", (string)row["description"]!, "Khôi phục từ thùng rác");
    // Khôi phục khoản thu thì khôi phục luôn tiền xưởng / tiền ship đã bị xóa cùng nó.
    if ((string)row["kind"]! == "INCOME")
        foreach (var linked in await store.LinkedExpenseIdsAsync(id, deleted: true))
            if (await store.GetAnyAsync(linked) is { } l && periodLock.Check(l["date"] as string) is null && await store.RestoreAsync(linked))
                await audit.LogAsync("EXPENSE", linked, "RESTORE", (string)l["description"]!, $"Khôi phục cùng khoản thu #{id}");
    return Results.Ok(await store.GetAnyAsync(id));
});
app.MapDelete("/api/v1/trash/{id:long}", async (long id, LedgerStore store, AttachmentStore files, AuditStore audit) =>
{
    var row = await store.GetAnyAsync(id);
    if (row is null || row["deletedAt"] is null) return Results.NotFound();
    await PurgeAsync([row], store, files, audit, "Xóa vĩnh viễn khỏi thùng rác");
    return Results.NoContent();
});
app.MapDelete("/api/v1/trash", async (LedgerStore store, AttachmentStore files, AuditStore audit) =>
{
    var rows = await store.ListDeletedAsync();
    await PurgeAsync(rows, store, files, audit, "Làm trống thùng rác");
    return Results.Ok(new { purged = rows.Count });
});

// ---------------------------------------------------------------- Lịch sử chỉnh sửa
app.MapGet("/api/v1/audit", async (HttpRequest req, AuditStore audit) =>
{
    var q = req.Query;
    var page = Math.Max(1, Int(q["page"].ToString(), 1));
    var size = Math.Clamp(Int(q["pageSize"].ToString(), 30), 1, 100);
    long? entityId = long.TryParse(q["entityId"], out var eid) ? eid : null;
    var result = await audit.ListAsync(page, size, q["entityType"], q["action"], entityId, q["search"], q["dateFrom"], q["dateTo"]);
    return Results.Ok(new { items = result.Items, page, pageSize = size, totalItems = result.Total,
        totalPages = (int)Math.Ceiling(result.Total / (double)size) });
});

// ---------------------------------------------------------------- Tỷ giá
app.MapGet("/api/v1/fx", async (FxService fx, FxMonthlyStore monthly) =>
{
    // Tháng hiện tại đã tự nhập tỷ giá thì hiển thị theo tỷ giá đó.
    var month = DateTime.Today.ToString("yyyy-MM", CultureInfo.InvariantCulture);
    if ((await monthly.AllAsync()).TryGetValue(month, out var manual) && manual > 0)
        return Results.Ok(new { eurToUsd = manual, usdToEur = decimal.Round(1 / manual, 6), date = month, auto = false, manual = true });
    var r = await fx.LatestAsync();
    return Results.Ok(new { eurToUsd = r.EurToUsd, usdToEur = decimal.Round(1 / r.EurToUsd, 6), date = r.Date, auto = r.Auto, manual = false });
});
app.MapGet("/api/v1/fx/monthly", async (FxMonthlyStore monthly) =>
    Results.Ok((await monthly.AllAsync()).Select(p => new { month = p.Key, eurToUsd = p.Value })));
// Bảng tỷ giá từng tháng trong khoảng from..to (yyyy-MM): tỷ giá tự nhập (nếu có) và tỷ giá ECB trung bình tháng để tham khảo.
app.MapGet("/api/v1/fx/months", async (HttpRequest req, FxService fx, FxMonthlyStore monthly) =>
{
    string from = req.Query["from"].ToString(), to = req.Query["to"].ToString();
    if (!FxMonthlyStore.IsMonth(from) || !FxMonthlyStore.IsMonth(to)) return Results.BadRequest(new { title = "Tháng phải có dạng yyyy-MM." });
    var start = DateOnly.ParseExact(from + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
    var end = DateOnly.ParseExact(to + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
    if (end < start) (start, end) = (end, start);
    if ((end.Year - start.Year) * 12 + end.Month - start.Month > 35) return Results.BadRequest(new { title = "Mỗi lần xem tối đa 36 tháng." });
    var manual = await monthly.AllAsync();
    var ecb = await fx.MonthlyAverageAsync(start, end.AddMonths(1).AddDays(-1));
    var rows = new List<object>();
    for (var m = end; m >= start; m = m.AddMonths(-1))
    {
        var key = m.ToString("yyyy-MM", CultureInfo.InvariantCulture);
        rows.Add(new { month = key, manual = manual.TryGetValue(key, out var r) ? r : (decimal?)null, ecb = ecb.TryGetValue(key, out var e) ? e : (decimal?)null });
    }
    return Results.Ok(rows);
});
app.MapPut("/api/v1/fx/monthly/{month}", async (string month, FxMonthlyInput input, FxMonthlyStore monthly) =>
{
    if (!FxMonthlyStore.IsMonth(month)) return Results.BadRequest(new { title = "Tháng phải có dạng yyyy-MM." });
    // Chặn gõ nhầm (vd 0,0005 hay 11513): 1 EUR luôn quanh 1 USD.
    if (input.EurToUsd is not (>= 0.5m and <= 3m))
        return Results.BadRequest(new { title = "Tỷ giá 1 EUR = ? USD phải trong khoảng 0,5 – 3 (ví dụ 1,1593). Kiểm tra lại dấu phẩy / dấu chấm." });
    var rate = decimal.Round(input.EurToUsd, 6);
    await monthly.SetAsync(month, rate);
    return Results.Ok(new { month, eurToUsd = rate });
});
app.MapDelete("/api/v1/fx/monthly/{month}", async (string month, FxMonthlyStore monthly) =>
    await monthly.DeleteAsync(month) ? Results.NoContent() : Results.NotFound());

// ---------------------------------------------------------------- Etsy
app.MapGet("/api/v1/etsy/status", async (EtsyService etsy) => Results.Ok(await etsy.StatusAsync()));
app.MapGet("/api/v1/etsy/connect", async (EtsyService etsy) => Results.Redirect(await etsy.AuthorizeUrlAsync()));
app.MapGet("/api/v1/etsy/callback", async (HttpRequest req, EtsyService etsy) =>
{
    if (!string.IsNullOrEmpty(req.Query["error"]))
        return Results.Redirect("/#/incomes?etsy=error");
    await etsy.HandleCallbackAsync(req.Query["code"].ToString(), req.Query["state"].ToString());
    return Results.Redirect("/#/incomes?etsy=connected");
});
app.MapPost("/api/v1/etsy/disconnect", (EtsyService etsy) => { etsy.Disconnect(); return Results.NoContent(); });
app.MapGet("/api/v1/etsy/orders", async (HttpRequest req, EtsyService etsy, LedgerStore store, FxMonthlyStore monthly) =>
{
    if (!DateOnly.TryParse(req.Query["from"], CultureInfo.InvariantCulture, out var from)
        || !DateOnly.TryParse(req.Query["to"], CultureInfo.InvariantCulture, out var to))
        return Results.BadRequest(new { title = "Hãy chọn khoảng ngày cần lấy đơn." });
    var orders = await etsy.FetchOrdersAsync(from, to, await monthly.AllAsync());
    var existing = await store.ExistingOrderCodesAsync("ETSY_STORE", orders.Select(o => o.ReceiptId).ToList());
    foreach (var o in orders) o.Imported = existing.Contains(o.ReceiptId);
    return Results.Ok(orders.OrderByDescending(o => o.Date).ToList());
});
app.MapPost("/api/v1/etsy/import", async (EtsyImportInput input, LedgerStore store, AuditStore audit, Microsoft.Extensions.Options.IOptionsMonitor<EtsyOptions> options) =>
{
    var orders = input.Orders ?? [];
    if (orders.Count == 0) return Results.BadRequest(new { title = "Chưa chọn đơn nào để nhập." });
    var (imported, skipped) = await EtsyImporter.ImportAsync(orders, store, audit, options.CurrentValue, "Nhập từ Etsy");
    return Results.Ok(new { imported, skipped });
});
// Đồng bộ tự động: xem trạng thái, bật / tắt + chọn chu kỳ, chạy ngay.
app.MapGet("/api/v1/etsy/autosync", (EtsyAutoSync sync) => Results.Ok(sync.Status()));
app.MapPut("/api/v1/etsy/autosync", (EtsyAutoSyncInput input, EtsyAutoSync sync) =>
{
    sync.Configure(input.Enabled, input.IntervalMinutes);
    return Results.Ok(sync.Status());
});
app.MapPost("/api/v1/etsy/autosync/run", async (EtsyAutoSync sync, EtsyService etsy) =>
{
    if (!etsy.Ready) return Results.BadRequest(new { title = "Chưa kết nối Etsy: điền key và bấm \"Kết nối Etsy\" trước." });
    await sync.RunOnceAsync();
    return Results.Ok(sync.Status());
});

Register("incomes", "INCOME");
Register("expenses", "EXPENSE");

try
{
    await EnsureSchemaAsync(app.Configuration);
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    await PurgeExpiredAsync(sp.GetRequiredService<LedgerStore>(), sp.GetRequiredService<AttachmentStore>(), sp.GetRequiredService<AuditStore>());
}
catch (MySqlException error) { app.Logger.LogWarning(error, "Chưa chuẩn bị được database (MySQL chưa sẵn sàng)."); }
app.Run();

// ---------------------------------------------------------------- Hàm dùng chung

// Tạo các bảng mới nếu database cũ chưa có.
static async Task EnsureSchemaAsync(IConfiguration config)
{
    await using var db = new MySqlConnection(config.GetConnectionString("MySql"));
    await db.OpenAsync();
    foreach (var sql in new[] { AttachmentStore.CreateTableSql, AuditStore.CreateTableSql, FxMonthlyStore.CreateTableSql, CategoryGroupStore.CreateTableSql,
        "ALTER TABLE ledger_entries ADD COLUMN IF NOT EXISTS source VARCHAR(20) NULL AFTER payment_method",
        // Phạm vi khoản chi thành danh sách sửa được (categories.kind = 'SCOPE'); lần đầu tạo sẵn "Nội địa", "Quốc tế".
        "INSERT INTO categories (kind,name) SELECT 'SCOPE', x.name FROM (SELECT 'Nội địa' AS name UNION ALL SELECT 'Quốc tế') x "
            + "WHERE NOT EXISTS (SELECT 1 FROM categories WHERE kind='SCOPE')",
        // Dữ liệu cũ lưu mã DOMESTIC / INTERNATIONAL: đổi sang id của phạm vi tương ứng.
        "UPDATE ledger_entries e JOIN categories c ON c.kind='SCOPE' AND c.name='Nội địa' SET e.origin_scope=CAST(c.id AS CHAR) WHERE e.origin_scope='DOMESTIC'",
        "UPDATE ledger_entries e JOIN categories c ON c.kind='SCOPE' AND c.name='Quốc tế' SET e.origin_scope=CAST(c.id AS CHAR) WHERE e.origin_scope='INTERNATIONAL'" })
    {
        await using var cmd = new MySqlCommand(sql, db);
        await cmd.ExecuteNonQueryAsync();
    }
}

// Xóa hẳn các khoản nằm trong thùng rác quá hạn.
static async Task PurgeExpiredAsync(LedgerStore store, AttachmentStore files, AuditStore audit)
{
    var ids = await store.ExpiredTrashIdsAsync(TrashRetentionDays);
    if (ids.Count == 0) return;
    var rows = new List<Dictionary<string, object?>>();
    foreach (var id in ids) if (await store.GetAnyAsync(id) is { } row) rows.Add(row);
    await PurgeAsync(rows, store, files, audit, $"Tự xóa sau {TrashRetentionDays} ngày trong thùng rác");
}

static async Task PurgeAsync(List<Dictionary<string, object?>> rows, LedgerStore store, AttachmentStore files, AuditStore audit, string reason)
{
    var ids = rows.Select(r => (long)r["id"]!).ToList();
    await files.DeleteFilesOfEntriesAsync(ids);
    await store.PurgeAsync(ids);
    foreach (var row in rows)
        await audit.LogAsync((string)row["kind"]!, (long)row["id"]!, "PURGE", (string)row["description"]!, reason);
}

void Register(string route, string kind)
{
    var group = app.MapGroup("/api/v1/" + route);
    group.MapGet("", async (HttpRequest req, LedgerStore store) =>
    {
        var page = Math.Max(1, Int(req.Query["page"].ToString(), 1));
        var size = Math.Clamp(Int(req.Query["pageSize"].ToString(), 20), 1, 100);
        var result = await store.ListAsync(kind, page, size, req.Query["search"].ToString(),
            req.Query["dateFrom"].ToString(), req.Query["dateTo"].ToString());
        return Results.Ok(new { items = result.Items, page, pageSize = size,
            totalItems = result.Total, totalPages = (int)Math.Ceiling(result.Total / (double)size) });
    });
    group.MapGet("/{id:long}", async (long id, LedgerStore store) =>
        await store.GetAsync(kind, id) is { } row ? Results.Ok(row) : Results.NotFound());
    group.MapPost("", async (LedgerInput input, LedgerStore store, AuditStore audit, PeriodLock periodLock) =>
    {
        var error = await ValidateAsync(input, kind, store);
        if (error is not null) return Results.BadRequest(new { title = error });
        if (periodLock.Check(input.Date) is { } locked) return Results.Conflict(new { title = locked });
        var id = await store.InsertAsync(kind, input);
        var row = await store.GetAsync(kind, id);
        await audit.LogAsync(kind, id, "CREATE", input.Description.Trim(), null, AuditStore.Diff(null, row));
        return Results.Created($"/api/v1/{route}/{id}", row);
    });
    group.MapPut("/{id:long}", async (long id, LedgerInput input, LedgerStore store, AuditStore audit, PeriodLock periodLock) =>
    {
        var error = await ValidateAsync(input, kind, store);
        if (error is not null) return Results.BadRequest(new { title = error });
        var before = await store.GetAsync(kind, id);
        if (before is null) return Results.NotFound();
        if (before["source"] as string == EtsySource)
            return Results.Conflict(new { title = "Khoản nhập từ Etsy không được sửa, chỉ có thể xóa." });
        // Form sửa không gửi liên kết với khoản thu: giữ nguyên để tiền xưởng / tiền ship vẫn hiện ở khoản thu.
        if (before.TryGetValue("linkedIncomeId", out var link) && link is JsonElement linkValue)
            input.Extra["linkedIncomeId"] = linkValue;
        // Không sửa khoản của tháng đã khóa, cũng không chuyển khoản sang tháng đã khóa.
        if ((periodLock.Check(before["date"] as string) ?? periodLock.Check(input.Date)) is { } locked)
            return Results.Conflict(new { title = locked });
        await store.UpdateAsync(kind, id, input);
        var after = await store.GetAsync(kind, id);
        var changes = AuditStore.Diff(before, after);
        if (changes.Count > 0) await audit.LogAsync(kind, id, "UPDATE", input.Description.Trim(), null, changes);
        return Results.Ok(after);
    });
    group.MapDelete("/{id:long}", async (long id, LedgerStore store, AuditStore audit, PeriodLock periodLock) =>
    {
        var row = await store.GetAsync(kind, id);
        if (row is null) return Results.NotFound();
        if (periodLock.Check(row["date"] as string) is { } locked) return Results.Conflict(new { title = locked });
        if (!await store.DeleteAsync(kind, id)) return Results.NotFound();
        await audit.LogAsync(kind, id, "DELETE", (string)row["description"]!, "Chuyển vào thùng rác");
        // Xóa khoản thu thì tiền xưởng / tiền ship gắn với nó cũng vào thùng rác (trừ khoản thuộc kỳ đã khóa).
        if (kind == "INCOME")
            foreach (var linked in await store.LinkedExpenseIdsAsync(id))
                if (await store.GetAsync("EXPENSE", linked) is { } l && periodLock.Check(l["date"] as string) is null && await store.DeleteAsync("EXPENSE", linked))
                    await audit.LogAsync("EXPENSE", linked, "DELETE", (string)l["description"]!, $"Xóa cùng khoản thu #{id}");
        return Results.NoContent();
    });
    group.MapGet("/{id:long}/attachments", async (long id, LedgerStore store, AttachmentStore files) =>
        await store.GetAsync(kind, id) is null ? Results.NotFound() : Results.Ok(await files.ListAsync(id)));
    group.MapPost("/{id:long}/attachments", async (long id, HttpRequest req, LedgerStore store, AttachmentStore files, AuditStore audit, PeriodLock periodLock) =>
    {
        var entry = await store.GetAsync(kind, id);
        if (entry is null) return Results.NotFound();
        if (periodLock.Check(entry["date"] as string) is { } locked) return Results.Conflict(new { title = locked });
        if (!req.HasFormContentType) return Results.BadRequest(new { title = "Hãy gửi file dạng multipart/form-data." });
        var form = await req.ReadFormAsync();
        if (form.Files.Count == 0) return Results.BadRequest(new { title = "Chưa chọn file nào." });
        if (form.Files.Count > AttachmentStore.MaxFilesPerUpload)
            return Results.BadRequest(new { title = $"Mỗi lần chỉ tải tối đa {AttachmentStore.MaxFilesPerUpload} file." });
        foreach (var f in form.Files)
        {
            var error = AttachmentStore.Validate(f);
            if (error is not null) return Results.BadRequest(new { title = error });
        }
        var saved = new List<AttachmentInfo>();
        foreach (var f in form.Files)
        {
            var info = await files.SaveAsync(id, f);
            saved.Add(info);
            await audit.LogAsync(kind, id, "ATTACH_ADD", (string)entry["description"]!, info.OriginalName);
        }
        return Results.Ok(saved);
    });
}
static string? Kind(string type) => type.ToLowerInvariant() switch
{
    "income" => "INCOME", "expense" => "EXPENSE", "scope" => "SCOPE", _ => null
};
static string KindLabel(string kind) => kind switch { "INCOME" => "Loại thu", "SCOPE" => "Phạm vi khoản chi", _ => "Loại chi" };
static int Int(string value, int fallback) => int.TryParse(value, out var result) ? result : fallback;
static async Task<string?> ValidateAsync(LedgerInput input, string kind, LedgerStore store)
{
    if (!DateOnly.TryParseExact(input.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _) || string.IsNullOrWhiteSpace(input.Description)
        || input.Description.Length > 500 || input.CategoryId <= 0 || input.Amount < 0
        || input.TaxPercent is < 0 or > 100)
        return "Kiểm tra lại ngày, mô tả, danh mục, số tiền và thuế.";
    if (!await store.CategoryExistsAsync(input.CategoryId, kind)) return "Danh mục không thuộc loại thu/chi đã chọn.";
    return null;
}

public sealed class CategoryGroupInput
{
    public string? Name { get; set; }
    public List<long>? CategoryIds { get; set; }
}

public sealed class IncomeCostInput
{
    /// <summary>Số tiền đã trả (gồm thuế), USD.</summary>
    public decimal Amount { get; set; }
    public decimal TaxPercent { get; set; }
    public string? InvoiceId { get; set; }
    public string? Date { get; set; }
}

public sealed class FxMonthlyInput
{
    public decimal EurToUsd { get; set; }
}

public sealed class EtsyAutoSyncInput
{
    public bool Enabled { get; set; }
    public int IntervalMinutes { get; set; } = 60;
}

public sealed class EtsyImportInput
{
    public List<EtsyOrder>? Orders { get; set; }
}
