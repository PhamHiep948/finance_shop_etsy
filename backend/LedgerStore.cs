using System.Text.Json;
using MySqlConnector;

public sealed class LedgerStore(IConfiguration config)
{
    private MySqlConnection Open() => new(config.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:MySql"));
    private const string Select = "SELECT id, kind, date, description, category_id, amount, tax_percent, amount_after_tax, currency_code, order_code, sale_region, sales_channel, product_qty, payee, origin_scope, payment_method, details, created_at, updated_at, deleted_at, (SELECT COUNT(*) FROM attachments a WHERE a.entry_id=ledger_entries.id) AS attachment_count, source FROM ledger_entries";
    private static readonly string[] DetailKeys = ["referenceCode", "unitPrice", "itemTotal", "discountAmount", "discountCode", "subtotal", "shippingAmount", "taxAmount", "note"];

    public async Task<List<object>> CategoriesAsync(string kind)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT c.id, c.name, (SELECT COUNT(*) FROM ledger_entries e WHERE e.category_id=c.id AND e.deleted_at IS NULL) FROM categories c WHERE c.kind=@kind ORDER BY c.id", db);
        cmd.Parameters.AddWithValue("@kind", kind);
        var rows = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows.Add(new { id = reader.GetInt64(0), name = reader.GetString(1), used = reader.GetInt64(2) });
        return rows;
    }
    /// <summary>Thêm loại; trả null nếu trùng tên.</summary>
    public async Task<long?> InsertCategoryAsync(string kind, string name)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("INSERT INTO categories (kind,name) VALUES (@kind,@name)", db);
        cmd.Parameters.AddWithValue("@kind", kind); cmd.Parameters.AddWithValue("@name", name);
        try { await cmd.ExecuteNonQueryAsync(); return cmd.LastInsertedId; }
        catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry) { return null; }
    }
    /// <summary>Đổi tên loại; trả false nếu trùng tên.</summary>
    public async Task<bool> RenameCategoryAsync(long id, string name)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("UPDATE categories SET name=@name WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@name", name);
        try { await cmd.ExecuteNonQueryAsync(); return true; }
        catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry) { return false; }
    }
    public async Task<long> CategoryUsageAsync(long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM ledger_entries WHERE category_id=@id AND deleted_at IS NULL", db);
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }
    /// <summary>Số khoản trong thùng rác còn dùng loại này.</summary>
    public async Task<long> CategoryTrashCountAsync(long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM ledger_entries WHERE category_id=@id AND deleted_at IS NOT NULL", db);
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync());
    }
    public async Task<long> FirstCategoryIdAsync(string kind)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT id FROM categories WHERE kind=@kind ORDER BY id LIMIT 1", db);
        cmd.Parameters.AddWithValue("@kind", kind);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync() ?? 0L);
    }
    public async Task<string?> CategoryNameAsync(long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT name FROM categories WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteScalarAsync() as string;
    }
    public async Task DeleteCategoryAsync(long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("DELETE FROM categories WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<bool> CategoryExistsAsync(long id, string kind)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM categories WHERE id=@id AND kind=@kind", db);
        cmd.Parameters.AddWithValue("@id", id); cmd.Parameters.AddWithValue("@kind", kind);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
    }
    public async Task<(List<Dictionary<string, object?>> Items, long Total)> ListAsync(
        string kind, int page, int size, string search, string dateFrom, string dateTo)
    {
        await using var db = Open(); await db.OpenAsync();
        var where = " WHERE kind=@kind AND deleted_at IS NULL";
        if (!string.IsNullOrWhiteSpace(search)) where += " AND description LIKE @search";
        if (DateOnly.TryParse(dateFrom, out _)) where += " AND date >= @dateFrom";
        if (DateOnly.TryParse(dateTo, out _)) where += " AND date <= @dateTo";
        await using var count = new MySqlCommand("SELECT COUNT(*) FROM ledger_entries" + where, db);
        AddFilters(count, kind, search, dateFrom, dateTo);
        var total = Convert.ToInt64(await count.ExecuteScalarAsync());
        await using var cmd = new MySqlCommand(Select + where + " ORDER BY date DESC,id DESC LIMIT @size OFFSET @offset", db);
        AddFilters(cmd, kind, search, dateFrom, dateTo);
        cmd.Parameters.AddWithValue("@size", size); cmd.Parameters.AddWithValue("@offset", (page - 1) * size);
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return (rows, total);
    }
    private static void AddFilters(MySqlCommand cmd, string kind, string search, string from, string to)
    {
        cmd.Parameters.AddWithValue("@kind", kind);
        if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@search", "%" + search.Trim() + "%");
        if (DateOnly.TryParse(from, out var fromDate)) cmd.Parameters.AddWithValue("@dateFrom", fromDate.ToDateTime(TimeOnly.MinValue));
        if (DateOnly.TryParse(to, out var toDate)) cmd.Parameters.AddWithValue("@dateTo", toDate.ToDateTime(TimeOnly.MinValue));
    }
    public async Task<Dictionary<string, object?>?> GetAsync(string kind, long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand(Select + " WHERE kind=@kind AND id=@id AND deleted_at IS NULL", db);
        cmd.Parameters.AddWithValue("@kind", kind); cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }
    private static Dictionary<string, object?> Map(MySqlDataReader r)
    {
        object? Optional(int i) => r.IsDBNull(i) ? null : r.GetValue(i);
        string? Text(int i) => r.IsDBNull(i) ? null : r.GetString(i);
        var row = new Dictionary<string, object?> {
            ["id"] = r.GetInt64(0), ["kind"] = r.GetString(1),
            ["date"] = r.GetDateTime(2).ToString("yyyy-MM-dd"), ["description"] = r.GetString(3),
            ["categoryId"] = r.GetInt64(4), ["amount"] = r.GetDecimal(5),
            ["taxPercent"] = r.GetDecimal(6), ["amountAfterTax"] = r.GetDecimal(7),
            ["currencyCode"] = r.GetString(8), ["orderCode"] = Text(9),
            ["saleRegion"] = Text(10), ["salesChannel"] = Text(11),
            ["productQty"] = Optional(12), ["payee"] = Text(13),
            ["originScope"] = Text(14), ["paymentMethod"] = Text(15),
            ["createdAt"] = r.GetDateTime(17), ["updatedAt"] = r.GetDateTime(18),
            ["deletedAt"] = Optional(19), ["attachmentCount"] = Convert.ToInt32(r.GetValue(20)),
            ["source"] = Text(21)
        };
        var json = Text(16);
        if (!string.IsNullOrWhiteSpace(json))
        {
            using var document = JsonDocument.Parse(json);
            foreach (var property in document.RootElement.EnumerateObject())
                row[property.Name] = property.Value.Clone();
        }
        return row;
    }
    private static MySqlCommand WriteCommand(MySqlConnection db, LedgerInput input, string kind, long? id, string? source = null)
    {
        var columns = new[] { "date", "description", "category_id", "amount", "tax_percent", "amount_after_tax", "currency_code", "order_code", "sale_region", "sales_channel", "product_qty", "payee", "origin_scope", "payment_method", "details" };
        // Nguồn (vd. ETSY) chỉ ghi lúc tạo, không bao giờ đổi khi sửa.
        if (id is null) columns = [.. columns, "source"];
        var sql = id is null
            ? "INSERT INTO ledger_entries (kind," + string.Join(",", columns) + ") VALUES (@kind," + string.Join(",", columns.Select(x => "@" + x)) + ")"
            : "UPDATE ledger_entries SET " + string.Join(",", columns.Select(x => x + "=@" + x)) + " WHERE kind=@kind AND id=@id AND deleted_at IS NULL";
        var cmd = new MySqlCommand(sql, db);
        cmd.Parameters.AddWithValue("@kind", kind);
        if (id is null) cmd.Parameters.AddWithValue("@source", (object?)source ?? DBNull.Value);
        if (id is not null) cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@date", input.Date);
        cmd.Parameters.AddWithValue("@description", input.Description.Trim());
        cmd.Parameters.AddWithValue("@category_id", input.CategoryId);
        cmd.Parameters.AddWithValue("@amount", input.Amount);
        cmd.Parameters.AddWithValue("@tax_percent", input.TaxPercent);
        cmd.Parameters.AddWithValue("@amount_after_tax", decimal.Round(input.Amount * (1 + input.TaxPercent / 100), 2, MidpointRounding.AwayFromZero));
        cmd.Parameters.AddWithValue("@currency_code", "USD");
        cmd.Parameters.AddWithValue("@order_code", (object?)input.OrderCode ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sale_region", (object?)input.SaleRegion ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sales_channel", (object?)input.SalesChannel ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@product_qty", (object?)input.ProductQty ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@payee", (object?)input.Payee ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@origin_scope", (object?)input.OriginScope ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@payment_method", (object?)input.PaymentMethod ?? DBNull.Value);
        var details = input.Extra.Where(pair => DetailKeys.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        cmd.Parameters.AddWithValue("@details", JsonSerializer.Serialize(details));
        return cmd;
    }
    public async Task<long> InsertAsync(string kind, LedgerInput input, string? source = null)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = WriteCommand(db, input, kind, null, source);
        await cmd.ExecuteNonQueryAsync();
        return cmd.LastInsertedId;
    }
    public async Task UpdateAsync(string kind, long id, LedgerInput input)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = WriteCommand(db, input, kind, id);
        await cmd.ExecuteNonQueryAsync();
    }
    public async Task<bool> DeleteAsync(string kind, long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("UPDATE ledger_entries SET deleted_at=NOW() WHERE kind=@kind AND id=@id AND deleted_at IS NULL", db);
        cmd.Parameters.AddWithValue("@kind", kind); cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    // ---- Thùng rác (khoản đã xóa mềm)
    /// <summary>Lấy bản ghi theo id, kể cả khi đang nằm trong thùng rác.</summary>
    public async Task<Dictionary<string, object?>?> GetAnyAsync(long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand(Select + " WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }
    public async Task<List<Dictionary<string, object?>>> ListDeletedAsync()
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand(Select + " WHERE deleted_at IS NOT NULL ORDER BY deleted_at DESC, id DESC", db);
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }
    public async Task<bool> RestoreAsync(long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("UPDATE ledger_entries SET deleted_at=NULL WHERE id=@id AND deleted_at IS NOT NULL", db);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
    /// <summary>Xóa vĩnh viễn các khoản đang trong thùng rác.</summary>
    public async Task<int> PurgeAsync(IReadOnlyCollection<long> ids)
    {
        if (ids.Count == 0) return 0;
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand(
            $"DELETE FROM ledger_entries WHERE deleted_at IS NOT NULL AND id IN ({string.Join(",", ids)})", db);
        return await cmd.ExecuteNonQueryAsync();
    }
    /// <summary>Id các khoản đã nằm trong thùng rác quá số ngày cho phép.</summary>
    public async Task<List<long>> ExpiredTrashIdsAsync(int days)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT id FROM ledger_entries WHERE deleted_at IS NOT NULL AND deleted_at < NOW() - INTERVAL @days DAY", db);
        cmd.Parameters.AddWithValue("@days", days);
        var ids = new List<long>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) ids.Add(reader.GetInt64(0));
        return ids;
    }
    /// <summary>Các mã đơn đã có trong sổ (kể cả trong thùng rác) cho một kênh bán.</summary>
    public async Task<HashSet<string>> ExistingOrderCodesAsync(string salesChannel, IReadOnlyCollection<string> codes)
    {
        var found = new HashSet<string>();
        if (codes.Count == 0) return found;
        await using var db = Open(); await db.OpenAsync();
        var names = codes.Select((_, i) => "@c" + i).ToList();
        await using var cmd = new MySqlCommand(
            $"SELECT order_code FROM ledger_entries WHERE kind='INCOME' AND sales_channel=@channel AND order_code IN ({string.Join(",", names)})", db);
        cmd.Parameters.AddWithValue("@channel", salesChannel);
        var i = 0;
        foreach (var code in codes) cmd.Parameters.AddWithValue(names[i++], code);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) found.Add(reader.GetString(0));
        return found;
    }
}
