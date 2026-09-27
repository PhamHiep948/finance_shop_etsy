using System.Globalization;
using System.Text.Json;
using MySqlConnector;

/// <summary>
/// Lịch sử chỉnh sửa. Mỗi dòng ghi một thao tác: thêm / sửa / xóa / khôi phục / xóa vĩnh viễn khoản thu-chi,
/// thêm-xóa chứng từ, thêm-đổi tên-xóa loại. Cột changes lưu {"field": {"from": ..., "to": ...}}.
/// </summary>
public sealed class AuditStore(IConfiguration config)
{
    public const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS audit_logs (
         id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
         entity_type VARCHAR(20) NOT NULL,
         entity_id BIGINT UNSIGNED NOT NULL,
         action VARCHAR(20) NOT NULL,
         label VARCHAR(500) NOT NULL,
         detail VARCHAR(500) NULL,
         changes JSON NULL,
         created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
         INDEX idx_audit_created (created_at),
         INDEX idx_audit_entity (entity_type, entity_id)
        ) ENGINE=InnoDB
        """;

    /// <summary>Các trường của khoản thu/chi được theo dõi thay đổi.</summary>
    public static readonly string[] TrackedFields =
    [
        "date", "description", "categoryId", "amount", "taxPercent", "amountAfterTax", "orderCode", "saleRegion",
        "salesChannel", "productQty", "payee", "originScope", "paymentMethod",
        "referenceCode", "unitPrice", "itemTotal", "discountAmount", "discountCode", "subtotal", "shippingAmount", "taxAmount", "note",
    ];

    private MySqlConnection Open() => new(config.GetConnectionString("MySql"));

    public async Task LogAsync(string entityType, long entityId, string action, string label,
        string? detail = null, Dictionary<string, object?>? changes = null)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand(
            "INSERT INTO audit_logs (entity_type,entity_id,action,label,detail,changes) VALUES (@type,@id,@action,@label,@detail,@changes)", db);
        cmd.Parameters.AddWithValue("@type", entityType);
        cmd.Parameters.AddWithValue("@id", entityId);
        cmd.Parameters.AddWithValue("@action", action);
        cmd.Parameters.AddWithValue("@label", label.Length > 500 ? label[..500] : label);
        cmd.Parameters.AddWithValue("@detail", (object?)detail ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@changes", changes is { Count: > 0 } ? JsonSerializer.Serialize(changes) : DBNull.Value);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <summary>So sánh hai bản ghi (dạng dictionary từ LedgerStore) trên các trường được theo dõi.</summary>
    public static Dictionary<string, object?> Diff(Dictionary<string, object?>? before, Dictionary<string, object?>? after)
    {
        var changes = new Dictionary<string, object?>();
        foreach (var field in TrackedFields)
        {
            var from = Norm(before?.GetValueOrDefault(field));
            var to = Norm(after?.GetValueOrDefault(field));
            if (from != to) changes[field] = new { from, to };
        }
        return changes;
    }

    /// <summary>Đưa mọi giá trị về chuỗi để so sánh ("29.00" và 29 là như nhau, "" coi như null).</summary>
    private static string? Norm(object? value)
    {
        string? text = value switch
        {
            null => null,
            JsonElement { ValueKind: JsonValueKind.Null or JsonValueKind.Undefined } => null,
            JsonElement { ValueKind: JsonValueKind.String } e => e.GetString(),
            JsonElement e => e.GetRawText(),
            decimal d => d.ToString(CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString(),
        };
        text = text?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var number)
            ? number.ToString("0.##########", CultureInfo.InvariantCulture)
            : text;
    }

    public async Task<(List<Dictionary<string, object?>> Items, long Total)> ListAsync(
        int page, int size, string? entityType, string? action, long? entityId, string? search, string? from, string? to)
    {
        await using var db = Open(); await db.OpenAsync();
        var where = new List<string>();
        var parameters = new List<(string Name, object Value)>();
        void Add(string sql, string name, object value) { where.Add(sql); parameters.Add((name, value)); }
        if (!string.IsNullOrWhiteSpace(entityType))
        {
            if (entityType == "ENTRY") where.Add("entity_type IN ('INCOME','EXPENSE')");
            else Add("entity_type=@type", "@type", entityType);
        }
        if (!string.IsNullOrWhiteSpace(action))
        {
            if (action == "ATTACH") where.Add("action IN ('ATTACH_ADD','ATTACH_DELETE')");
            else Add("action=@action", "@action", action);
        }
        if (entityId is not null) Add("entity_id=@entity", "@entity", entityId);
        if (!string.IsNullOrWhiteSpace(search)) Add("(label LIKE @search OR detail LIKE @search)", "@search", "%" + search.Trim() + "%");
        if (DateOnly.TryParse(from, out var f)) Add("created_at >= @from", "@from", f.ToDateTime(TimeOnly.MinValue));
        if (DateOnly.TryParse(to, out var t)) Add("created_at < @to", "@to", t.AddDays(1).ToDateTime(TimeOnly.MinValue));
        var clause = where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "";

        await using var count = new MySqlCommand("SELECT COUNT(*) FROM audit_logs" + clause, db);
        foreach (var p in parameters) count.Parameters.AddWithValue(p.Name, p.Value);
        var total = Convert.ToInt64(await count.ExecuteScalarAsync());

        await using var cmd = new MySqlCommand(
            "SELECT id, entity_type, entity_id, action, label, detail, changes, created_at FROM audit_logs" + clause +
            " ORDER BY created_at DESC, id DESC LIMIT @size OFFSET @offset", db);
        foreach (var p in parameters) cmd.Parameters.AddWithValue(p.Name, p.Value);
        cmd.Parameters.AddWithValue("@size", size);
        cmd.Parameters.AddWithValue("@offset", (page - 1) * size);
        var rows = new List<Dictionary<string, object?>>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            rows.Add(new Dictionary<string, object?>
            {
                ["id"] = reader.GetInt64(0),
                ["entityType"] = reader.GetString(1),
                ["entityId"] = reader.GetInt64(2),
                ["action"] = reader.GetString(3),
                ["label"] = reader.GetString(4),
                ["detail"] = reader.IsDBNull(5) ? null : reader.GetString(5),
                ["changes"] = reader.IsDBNull(6) ? null : JsonDocument.Parse(reader.GetString(6)).RootElement.Clone(),
                ["createdAt"] = reader.GetDateTime(7),
            });
        }
        return (rows, total);
    }
}
