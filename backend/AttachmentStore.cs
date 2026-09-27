using MySqlConnector;

/// <summary>Chứng từ đính kèm: thông tin lưu trong bảng attachments, file nằm trong thư mục backend/uploads.</summary>
public sealed class AttachmentStore(IConfiguration config, IWebHostEnvironment env)
{
    public const int MaxFilesPerUpload = 10;
    private const long MaxBytes = 10 * 1024 * 1024;
    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".gif"] = "image/gif",
        [".webp"] = "image/webp", [".pdf"] = "application/pdf", [".txt"] = "text/plain", [".csv"] = "text/csv",
        [".doc"] = "application/msword",
        [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        [".xls"] = "application/vnd.ms-excel",
        [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        [".zip"] = "application/zip",
    };
    public const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS attachments (
         id BIGINT UNSIGNED PRIMARY KEY AUTO_INCREMENT,
         entry_id BIGINT UNSIGNED NOT NULL,
         original_name VARCHAR(255) NOT NULL,
         stored_name VARCHAR(100) NOT NULL,
         content_type VARCHAR(150) NOT NULL,
         size_bytes BIGINT NOT NULL,
         created_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
         INDEX idx_attachment_entry (entry_id),
         CONSTRAINT fk_attachment_entry FOREIGN KEY (entry_id) REFERENCES ledger_entries(id) ON DELETE CASCADE
        ) ENGINE=InnoDB
        """;
    private const string Select = "SELECT id, entry_id, original_name, stored_name, content_type, size_bytes, created_at FROM attachments";

    private string Dir => System.IO.Path.Combine(env.ContentRootPath, "uploads");
    private MySqlConnection Open() => new(config.GetConnectionString("MySql"));

    public static bool CanPreview(string contentType) =>
        contentType.StartsWith("image/") || contentType is "application/pdf" or "text/plain";

    public static string? Validate(IFormFile file)
    {
        var ext = System.IO.Path.GetExtension(file.FileName);
        if (!Types.ContainsKey(ext)) return $"File \"{file.FileName}\" không được hỗ trợ. Chỉ nhận ảnh, PDF, Word, Excel, CSV, TXT, ZIP.";
        if (file.Length == 0) return $"File \"{file.FileName}\" rỗng.";
        if (file.Length > MaxBytes) return $"File \"{file.FileName}\" vượt quá 10 MB.";
        return null;
    }

    private AttachmentInfo Map(MySqlDataReader r) => new(r.GetInt64(0), r.GetInt64(1), r.GetString(2),
        r.GetString(4), r.GetInt64(5), r.GetDateTime(6)) { Path = System.IO.Path.Combine(Dir, r.GetString(3)) };

    public async Task<List<AttachmentInfo>> ListAsync(long entryId)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand(Select + " WHERE entry_id=@id ORDER BY id", db);
        cmd.Parameters.AddWithValue("@id", entryId);
        var rows = new List<AttachmentInfo>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rows.Add(Map(reader));
        return rows;
    }

    public async Task<AttachmentInfo?> GetAsync(long id)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand(Select + " WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id);
        await using var reader = await cmd.ExecuteReaderAsync();
        return await reader.ReadAsync() ? Map(reader) : null;
    }

    public async Task<AttachmentInfo> SaveAsync(long entryId, IFormFile file)
    {
        Directory.CreateDirectory(Dir);
        var ext = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        var stored = Guid.NewGuid().ToString("N") + ext;
        await using (var stream = File.Create(System.IO.Path.Combine(Dir, stored)))
            await file.CopyToAsync(stream);
        var name = System.IO.Path.GetFileName(file.FileName);
        if (name.Length > 255) name = name[^255..];
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("INSERT INTO attachments (entry_id,original_name,stored_name,content_type,size_bytes) VALUES (@entry,@name,@stored,@type,@size)", db);
        cmd.Parameters.AddWithValue("@entry", entryId);
        cmd.Parameters.AddWithValue("@name", name);
        cmd.Parameters.AddWithValue("@stored", stored);
        cmd.Parameters.AddWithValue("@type", Types[ext]);
        cmd.Parameters.AddWithValue("@size", file.Length);
        await cmd.ExecuteNonQueryAsync();
        return (await GetAsync(cmd.LastInsertedId))!;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var file = await GetAsync(id);
        if (file is null) return false;
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("DELETE FROM attachments WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id);
        await cmd.ExecuteNonQueryAsync();
        if (File.Exists(file.Path)) File.Delete(file.Path);
        return true;
    }

    /// <summary>Xóa file của các khoản sắp bị xóa vĩnh viễn (dòng DB tự xóa nhờ ON DELETE CASCADE).</summary>
    public async Task DeleteFilesOfEntriesAsync(IReadOnlyCollection<long> entryIds)
    {
        if (entryIds.Count == 0) return;
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand(
            $"SELECT stored_name FROM attachments WHERE entry_id IN ({string.Join(",", entryIds)})", db);
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var path = System.IO.Path.Combine(Dir, reader.GetString(0));
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
