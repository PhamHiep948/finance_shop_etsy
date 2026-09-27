using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.FileProviders;
using MySqlConnector;

const int TrashRetentionDays = 30;
const string EtsySource = "ETSY"; // Đánh dấu khoản thu nhập từ Etsy: chỉ xem / xóa, không sửa.

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
builder.Services.AddSingleton<FxService>();
builder.Services.AddSingleton<EtsyService>();
var app = builder.Build();
app.UseCors("FrontendLocal");
// Phục vụ giao diện HTML/CSS/JS trong thư mục ../frontend tại http://localhost:5000/
var frontendDir = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "frontend"));
if (Directory.Exists(frontendDir))
{
    var frontendFiles = new PhysicalFileProvider(frontendDir);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendFiles });
    app.UseStaticFiles(new StaticFileOptions { FileProvider = frontendFiles });
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
    var used = await store.CategoryUsageAsync(id);
    if (used > 0)
        return Results.Conflict(new { title = $"Loại này đang được dùng bởi {used} khoản. Hãy chuyển các khoản đó sang loại khác trước khi xóa." });
    var inTrash = await store.CategoryTrashCountAsync(id);
    if (inTrash > 0)
        return Results.Conflict(new { title = $"Thùng rác còn {inTrash} khoản thuộc loại này. Hãy khôi phục hoặc xóa vĩnh viễn chúng trước." });
    var name = await store.CategoryNameAsync(id) ?? "";
    await store.DeleteCategoryAsync(id);
    await audit.LogAsync("CATEGORY", id, "DELETE", name, KindLabel(kind));
    return Results.NoContent();
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
app.MapGet("/api/v1/fx", async (FxService fx) =>
{
    var r = await fx.LatestAsync();
    return Results.Ok(new { eurToUsd = r.EurToUsd, usdToEur = decimal.Round(1 / r.EurToUsd, 6), date = r.Date, auto = r.Auto });
});

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
app.MapGet("/api/v1/etsy/orders", async (HttpRequest req, EtsyService etsy, LedgerStore store) =>
{
    if (!DateOnly.TryParse(req.Query["from"], CultureInfo.InvariantCulture, out var from)
        || !DateOnly.TryParse(req.Query["to"], CultureInfo.InvariantCulture, out var to))
        return Results.BadRequest(new { title = "Hãy chọn khoảng ngày cần lấy đơn." });
    var orders = await etsy.FetchOrdersAsync(from, to);
    var existing = await store.ExistingOrderCodesAsync("ETSY_STORE", orders.Select(o => o.ReceiptId).ToList());
    foreach (var o in orders) o.Imported = existing.Contains(o.ReceiptId);
    return Results.Ok(orders.OrderByDescending(o => o.Date).ToList());
});
app.MapPost("/api/v1/etsy/import", async (EtsyImportInput input, LedgerStore store, AuditStore audit, PeriodLock periodLock, Microsoft.Extensions.Options.IOptionsMonitor<EtsyOptions> options) =>
{
    var orders = input.Orders ?? [];
    if (orders.Count == 0) return Results.BadRequest(new { title = "Chưa chọn đơn nào để nhập." });
    var categoryId = options.CurrentValue.DefaultCategoryId;
    if (!await store.CategoryExistsAsync(categoryId, "INCOME"))
        categoryId = await store.FirstCategoryIdAsync("INCOME");
    if (categoryId == 0) return Results.BadRequest(new { title = "Chưa có loại thu nào để gán cho đơn Etsy." });
    var existing = await store.ExistingOrderCodesAsync("ETSY_STORE", orders.Select(o => o.ReceiptId).ToList());
    var imported = 0;
    var lockedCount = 0;
    foreach (var o in orders.Where(o => !existing.Contains(o.ReceiptId)).DistinctBy(o => o.ReceiptId))
    {
        if (periodLock.Check(o.Date) is not null) { lockedCount++; continue; }
        var entry = new LedgerInput
        {
            Date = o.Date, Description = o.Description, CategoryId = categoryId,
            Amount = o.Amount, TaxPercent = o.TaxPercent, OrderCode = o.ReceiptId,
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
        var error = await ValidateAsync(entry, "INCOME", store);
        if (error is not null) continue;
        var id = await store.InsertAsync("INCOME", entry, EtsySource);
        await audit.LogAsync("INCOME", id, "CREATE", entry.Description, "Nhập từ Etsy",
            AuditStore.Diff(null, await store.GetAsync("INCOME", id)));
        imported++;
    }
    return Results.Ok(new { imported, locked = lockedCount, skipped = orders.Count - imported - lockedCount });
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
    foreach (var sql in new[] { AttachmentStore.CreateTableSql, AuditStore.CreateTableSql,
        "ALTER TABLE ledger_entries ADD COLUMN IF NOT EXISTS source VARCHAR(20) NULL AFTER payment_method" })
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
    "income" => "INCOME", "expense" => "EXPENSE", _ => null
};
static string KindLabel(string kind) => kind == "INCOME" ? "Loại thu" : "Loại chi";
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

public sealed class EtsyImportInput
{
    public List<EtsyOrder>? Orders { get; set; }
}
