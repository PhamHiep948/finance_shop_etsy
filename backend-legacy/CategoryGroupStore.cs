using MySqlConnector;

/// <summary>
/// Nhóm loại thu / loại chi do người dùng đặt tên (ví dụ "Chi phí Etsy" = Quảng cáo + Phí dịch vụ),
/// dùng để chọn nhanh nhiều loại cùng lúc trong bộ lọc báo cáo.
/// </summary>
public sealed class CategoryGroupStore(IConfiguration config)
{
    public const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS category_groups (
         id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
         name VARCHAR(150) NOT NULL,
         category_ids TEXT NOT NULL,
         created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
         UNIQUE KEY uq_group_name (name)
        ) ENGINE=InnoDB
        """;

    private MySqlConnection Open() => new(config.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:MySql"));

    private static string Join(IEnumerable<long> ids) => string.Join(",", ids.Where(x => x > 0).Distinct().Order());
    private static List<long> Split(string text) =>
        text.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => long.TryParse(x, out var v) ? v : 0).Where(x => x > 0).ToList();

    public async Task<List<object>> AllAsync()
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT id, name, category_ids FROM category_groups ORDER BY id", db);
        var rows = new List<object>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
            rows.Add(new { id = reader.GetInt64(0), name = reader.GetString(1), categoryIds = Split(reader.GetString(2)) });
        return rows;
    }

    public async Task<bool> ExistsAsync(long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT COUNT(*) FROM category_groups WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id);
        return Convert.ToInt64(await cmd.ExecuteScalarAsync()) > 0;
    }

    /// <summary>Thêm nhóm; trả null nếu trùng tên.</summary>
    public async Task<long?> InsertAsync(string name, IEnumerable<long> categoryIds)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("INSERT INTO category_groups (name, category_ids) VALUES (@name, @ids)", db);
        cmd.Parameters.AddWithValue("@name", name); cmd.Parameters.AddWithValue("@ids", Join(categoryIds));
        try { await cmd.ExecuteNonQueryAsync(); return cmd.LastInsertedId; }
        catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry) { return null; }
    }

    /// <summary>Đổi tên và / hoặc danh sách loại (null = giữ nguyên); trả false nếu trùng tên.</summary>
    public async Task<bool> UpdateAsync(long id, string? name, IEnumerable<long>? categoryIds)
    {
        var sets = new List<string>();
        if (name is not null) sets.Add("name=@name");
        if (categoryIds is not null) sets.Add("category_ids=@ids");
        if (sets.Count == 0) return true;
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand($"UPDATE category_groups SET {string.Join(",", sets)} WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id);
        if (name is not null) cmd.Parameters.AddWithValue("@name", name);
        if (categoryIds is not null) cmd.Parameters.AddWithValue("@ids", Join(categoryIds));
        try { await cmd.ExecuteNonQueryAsync(); return true; }
        catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry) { return false; }
    }

    public async Task<bool> DeleteAsync(long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("DELETE FROM category_groups WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
