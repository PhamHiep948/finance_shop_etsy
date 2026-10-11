using System.Globalization;
using MySqlConnector;

/// <summary>
/// Tỷ giá EUR → USD người dùng tự nhập cho từng tháng (yyyy-MM).
/// Tháng nào đã nhập thì đơn Etsy bằng EUR của tháng đó quy đổi theo số này, thay cho tỷ giá ECB tự động.
/// </summary>
public sealed class FxMonthlyStore(IConfiguration config)
{
    public const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS fx_monthly (
         month CHAR(7) PRIMARY KEY,
         eur_to_usd DECIMAL(12,6) NOT NULL,
         updated_at DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
        )
        """;

    private MySqlConnection Open() => new(config.GetConnectionString("MySql")
        ?? throw new InvalidOperationException("Missing ConnectionStrings:MySql"));

    public static bool IsMonth(string? month) =>
        DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

    /// <summary>Tất cả tỷ giá đã nhập, khóa là "yyyy-MM".</summary>
    public async Task<Dictionary<string, decimal>> AllAsync()
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("SELECT month, eur_to_usd FROM fx_monthly ORDER BY month DESC", db);
        var rates = new Dictionary<string, decimal>();
        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync()) rates[reader.GetString(0)] = reader.GetDecimal(1);
        return rates;
    }

    public async Task SetAsync(string month, decimal eurToUsd)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand(
            "INSERT INTO fx_monthly (month, eur_to_usd) VALUES (@month, @rate) ON DUPLICATE KEY UPDATE eur_to_usd=@rate", db);
        cmd.Parameters.AddWithValue("@month", month); cmd.Parameters.AddWithValue("@rate", eurToUsd);
        await cmd.ExecuteNonQueryAsync();
    }

    public async Task<bool> DeleteAsync(string month)
    {
        await using var db = Open(); await db.OpenAsync();
        await using var cmd = new MySqlCommand("DELETE FROM fx_monthly WHERE month=@month", db);
        cmd.Parameters.AddWithValue("@month", month);
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
}
