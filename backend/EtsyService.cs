using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

/// <summary>Cấu hình đọc từ backend/etsy.settings.json (mục "Etsy").</summary>
public sealed class EtsyOptions
{
    /// <summary>Keystring của ứng dụng Etsy (etsy.com/developers/your-apps).</summary>
    public string ApiKey { get; set; } = "";
    /// <summary>Shared secret của ứng dụng Etsy.</summary>
    public string SharedSecret { get; set; } = "";
    /// <summary>Shop ID dạng số. Để trống thì tự lấy từ tài khoản Etsy sau khi kết nối.</summary>
    public string ShopId { get; set; } = "";
    /// <summary>Phải trùng với Callback URL đã khai báo trong ứng dụng Etsy.</summary>
    public string RedirectUri { get; set; } = "http://localhost:5000/api/v1/etsy/callback";
    /// <summary>Loại thu gán cho đơn nhập từ Etsy (mặc định 1 = "Bán hàng").</summary>
    public long DefaultCategoryId { get; set; } = 1;
    /// <summary>Tỷ giá EUR → USD dự phòng, chỉ dùng khi không lấy được tỷ giá tự động (xem FxService).</summary>
    public decimal EurToUsd { get; set; } = 1.087m;
}

/// <summary>Một đơn Etsy đã quy đổi về dạng khoản thu (số tiền USD).</summary>
public sealed class EtsyOrder
{
    public string ReceiptId { get; set; } = "";
    public string Date { get; set; } = "";
    public string Description { get; set; } = "";
    public int Quantity { get; set; }
    public string Currency { get; set; } = "USD";
    public decimal ItemTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Shipping { get; set; }
    public decimal Tax { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxPercent { get; set; }
    public string Country { get; set; } = "";
    public string SaleRegion { get; set; } = "";
    public string? Note { get; set; }
    public bool Imported { get; set; }
}

public sealed class EtsyException(string message) : Exception(message);

public sealed class EtsyService(IOptionsMonitor<EtsyOptions> options, IHttpClientFactory http, IWebHostEnvironment env, FxService fx)
{
    private const string AuthorizeEndpoint = "https://www.etsy.com/oauth/connect";
    private const string TokenEndpoint = "https://api.etsy.com/v3/public/oauth/token";
    private const string ApiBase = "https://openapi.etsy.com/v3/application";
    private static readonly HashSet<string> EuCountries =
    [
        "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE",
        "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE",
    ];
    // state -> code_verifier của luồng OAuth PKCE đang chờ callback.
    private readonly Dictionary<string, string> pending = [];
    private readonly SemaphoreSlim tokenLock = new(1, 1);
    // Kết quả kiểm tra key gần nhất: key hợp lệ thì nhớ luôn, key sai thì kiểm tra lại sau 30 giây (để kịp nhận key vừa sửa).
    private (string Key, string? Error, DateTime CheckedAt)? keyCheck;

    private EtsyOptions O => options.CurrentValue;
    private string TokenFile => Path.Combine(env.ContentRootPath, "etsy.tokens.json");
    private bool HasKeys => !string.IsNullOrWhiteSpace(O.ApiKey) && !string.IsNullOrWhiteSpace(O.SharedSecret);

    public async Task<object> StatusAsync()
    {
        var keyError = HasKeys ? await CheckKeysAsync() : null;
        return new
        {
            configured = HasKeys,
            keyError,
            connected = File.Exists(TokenFile),
            shopId = string.IsNullOrWhiteSpace(O.ShopId) ? ReadToken()?.ShopId ?? "" : O.ShopId,
            redirectUri = O.RedirectUri,
            ready = HasKeys && keyError is null && File.Exists(TokenFile),
        };
    }

    /// <summary>Hỏi Etsy xem ApiKey + SharedSecret có hợp lệ không. Trả về null nếu hợp lệ, ngược lại là lời giải thích.</summary>
    public async Task<string?> CheckKeysAsync()
    {
        var key = ApiKeyHeader;
        if (keyCheck is { } c && c.Key == key && (c.Error is null || DateTime.UtcNow - c.CheckedAt < TimeSpan.FromSeconds(30)))
            return c.Error;
        string? error = null;
        try
        {
            using var client = http.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.Add("x-api-key", key);
            using var res = await client.GetAsync($"{ApiBase}/openapi-ping");
            if (!res.IsSuccessStatusCode)
            {
                var body = await res.Content.ReadAsStringAsync();
                error = (int)res.StatusCode is 401 or 403
                    ? "Etsy từ chối ApiKey / SharedSecret. Kiểm tra: (1) app trên etsy.com/developers/your-apps đã được duyệt (không còn Pending), "
                      + "(2) copy lại đúng Keystring và Shared Secret. Etsy trả lời: " + EtsyError(body)
                    : $"Etsy API lỗi ({(int)res.StatusCode}): {EtsyError(body)}";
            }
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            // Mất mạng: không kết luận key sai, cũng không nhớ kết quả.
            return "Không kết nối được tới Etsy (kiểm tra Internet).";
        }
        keyCheck = (key, error, DateTime.UtcNow);
        return error;
    }

    private string ApiKeyHeader => $"{O.ApiKey.Trim()}:{O.SharedSecret.Trim()}";

    /// <summary>Lấy câu báo lỗi trong JSON Etsy trả về ({"error": "..."}).</summary>
    private static string EtsyError(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var e) && e.ValueKind == JsonValueKind.String) return e.GetString()!;
        }
        catch (JsonException) { }
        return body.Length > 300 ? body[..300] : body;
    }

    // ---------------------------------------------------------------- OAuth (PKCE)

    public async Task<string> AuthorizeUrlAsync()
    {
        if (!HasKeys) throw new EtsyException("Chưa điền ApiKey và SharedSecret trong backend/etsy.settings.json.");
        if (await CheckKeysAsync() is { } keyError) throw new EtsyException(keyError);
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));
        lock (pending) pending[state] = verifier;
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["redirect_uri"] = O.RedirectUri,
            ["scope"] = "transactions_r shops_r",
            ["client_id"] = O.ApiKey.Trim(),
            ["state"] = state,
            ["code_challenge"] = challenge,
            ["code_challenge_method"] = "S256",
        };
        return AuthorizeEndpoint + "?" + string.Join("&", query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
    }

    public async Task HandleCallbackAsync(string code, string state)
    {
        string? verifier;
        lock (pending)
        {
            pending.Remove(state, out verifier);
        }
        if (verifier is null) throw new EtsyException("Phiên kết nối Etsy không hợp lệ hoặc đã hết hạn. Hãy bấm Kết nối lại.");
        var token = await RequestTokenAsync(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = O.ApiKey.Trim(),
            ["redirect_uri"] = O.RedirectUri,
            ["code"] = code,
            ["code_verifier"] = verifier,
        });
        if (string.IsNullOrWhiteSpace(O.ShopId)) await DetectShopIdAsync(token);
    }

    public void Disconnect()
    {
        if (File.Exists(TokenFile)) File.Delete(TokenFile);
    }

    // ShopId: lấy tự động từ tài khoản Etsy khi etsy.settings.json không điền.
    private sealed record TokenData(string AccessToken, string RefreshToken, DateTime ExpiresAt, string? ShopId = null);

    private TokenData? ReadToken()
    {
        try { return File.Exists(TokenFile) ? JsonSerializer.Deserialize<TokenData>(File.ReadAllText(TokenFile)) : null; }
        catch (Exception e) when (e is IOException or JsonException) { return null; }
    }

    private async Task<TokenData> RequestTokenAsync(Dictionary<string, string> form, string? shopId = null)
    {
        using var client = http.CreateClient();
        using var res = await client.PostAsync(TokenEndpoint, new FormUrlEncodedContent(form));
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            // Refresh token hết hạn / bị thu hồi: xóa token cũ để người dùng kết nối lại.
            if (form["grant_type"] == "refresh_token") Disconnect();
            throw new EtsyException($"Etsy từ chối cấp token ({(int)res.StatusCode}): {EtsyError(body)}. Hãy bấm \"Kết nối Etsy\" lại.");
        }
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;
        var token = new TokenData(
            root.GetProperty("access_token").GetString()!,
            root.GetProperty("refresh_token").GetString()!,
            DateTime.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32() - 60),
            shopId);
        await File.WriteAllTextAsync(TokenFile, JsonSerializer.Serialize(token));
        return token;
    }

    private async Task<TokenData> TokenAsync()
    {
        await tokenLock.WaitAsync();
        try
        {
            if (!File.Exists(TokenFile)) throw new EtsyException("Chưa kết nối tài khoản Etsy. Bấm \"Kết nối Etsy\" để đăng nhập.");
            var token = JsonSerializer.Deserialize<TokenData>(await File.ReadAllTextAsync(TokenFile))
                ?? throw new EtsyException("File token Etsy bị hỏng, hãy kết nối lại.");
            if (token.ExpiresAt > DateTime.UtcNow) return token;
            return await RequestTokenAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["client_id"] = O.ApiKey.Trim(),
                ["refresh_token"] = token.RefreshToken,
            }, token.ShopId);
        }
        finally
        {
            tokenLock.Release();
        }
    }

    private HttpClient ApiClient(string accessToken)
    {
        var client = http.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", ApiKeyHeader);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>Hỏi Etsy shop của tài khoản đang đăng nhập (GET /users/me) rồi lưu vào file token.</summary>
    private async Task<string> DetectShopIdAsync(TokenData token)
    {
        using var client = ApiClient(token.AccessToken);
        using var res = await client.GetAsync($"{ApiBase}/users/me");
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new EtsyException($"Không lấy được shop của tài khoản Etsy ({(int)res.StatusCode}): {EtsyError(body)}");
        using var doc = JsonDocument.Parse(body);
        if (!doc.RootElement.TryGetProperty("shop_id", out var id) || id.ValueKind != JsonValueKind.Number)
            throw new EtsyException("Tài khoản Etsy này chưa có shop. Hãy đăng nhập đúng tài khoản chủ shop.");
        var shopId = id.GetInt64().ToString(CultureInfo.InvariantCulture);
        await tokenLock.WaitAsync();
        try
        {
            var saved = ReadToken() ?? token;
            await File.WriteAllTextAsync(TokenFile, JsonSerializer.Serialize(saved with { ShopId = shopId }));
        }
        finally
        {
            tokenLock.Release();
        }
        return shopId;
    }

    // ---------------------------------------------------------------- Lấy đơn hàng

    public async Task<List<EtsyOrder>> FetchOrdersAsync(DateOnly from, DateOnly to)
    {
        if (to < from) (from, to) = (to, from);
        if (to.DayNumber - from.DayNumber > 366) throw new EtsyException("Mỗi lần chỉ lấy tối đa 1 năm đơn hàng.");
        var rates = await fx.DailyAsync(from, to);
        return await RealOrdersAsync(from, to, rates);
    }

    private async Task<List<EtsyOrder>> RealOrdersAsync(DateOnly from, DateOnly to, Func<DateOnly, FxService.FxRate> rates)
    {
        if (!HasKeys) throw new EtsyException("Chưa điền ApiKey và SharedSecret trong backend/etsy.settings.json.");
        var token = await TokenAsync();
        var shopId = !string.IsNullOrWhiteSpace(O.ShopId) ? O.ShopId : token.ShopId ?? await DetectShopIdAsync(token);
        using var client = ApiClient(token.AccessToken);
        var min = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds();
        var max = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue)).ToUnixTimeSeconds();
        var orders = new List<EtsyOrder>();
        for (var offset = 0; ; offset += 100)
        {
            var url = $"{ApiBase}/shops/{Uri.EscapeDataString(shopId)}/receipts?min_created={min}&max_created={max}&was_paid=true&was_canceled=false&limit=100&offset={offset}";
            using var res = await client.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    Disconnect();
                    throw new EtsyException("Phiên đăng nhập Etsy đã hết hạn hoặc bị thu hồi. Hãy bấm \"Kết nối Etsy\" lại.");
                }
                if (res.StatusCode == System.Net.HttpStatusCode.Forbidden)
                    throw new EtsyException($"Etsy không cho đọc đơn của shop {shopId}: {EtsyError(body)}. "
                        + "Kiểm tra đã đăng nhập đúng tài khoản chủ shop, và ShopId trong etsy.settings.json (nếu có điền) là đúng.");
                throw new EtsyException($"Etsy API lỗi ({(int)res.StatusCode}): {EtsyError(body)}");
            }
            using var doc = JsonDocument.Parse(body);
            var results = doc.RootElement.GetProperty("results");
            foreach (var receipt in results.EnumerateArray())
            {
                // Đơn đã hủy / hoàn tiền toàn bộ không còn là doanh thu.
                var status = receipt.TryGetProperty("status", out var st) ? st.GetString()?.ToLowerInvariant() : null;
                if (status is "canceled" or "fully refunded") continue;
                orders.Add(MapReceipt(receipt, rates));
            }
            if (results.GetArrayLength() < 100) break;
        }
        return orders;
    }

    private static EtsyOrder MapReceipt(JsonElement r, Func<DateOnly, FxService.FxRate> rates)
    {
        static decimal Money(JsonElement parent, string name)
        {
            if (!parent.TryGetProperty(name, out var m) || m.ValueKind != JsonValueKind.Object) return 0;
            var divisor = m.GetProperty("divisor").GetDecimal();
            return divisor == 0 ? 0 : m.GetProperty("amount").GetDecimal() / divisor;
        }
        var currency = r.TryGetProperty("grandtotal", out var g) && g.TryGetProperty("currency_code", out var c) ? c.GetString() ?? "USD" : "USD";
        var titles = r.TryGetProperty("transactions", out var tx)
            ? tx.EnumerateArray().Select(t => (Title: t.GetProperty("title").GetString() ?? "", Qty: t.TryGetProperty("quantity", out var q) ? q.GetInt32() : 1)).ToList()
            : [];
        var created = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(r.GetProperty("created_timestamp").GetInt64()).LocalDateTime);
        var order = Build(
            r.GetProperty("receipt_id").GetInt64().ToString(CultureInfo.InvariantCulture),
            created, titles, currency, rates(created),
            Money(r, "total_price"), Money(r, "discount_amt"), Money(r, "subtotal"),
            Money(r, "total_shipping_cost"), Money(r, "total_tax_cost") + Money(r, "total_vat_cost"),
            r.TryGetProperty("country_iso", out var country) ? country.GetString() ?? "" : "");
        // Hoàn tiền một phần: giữ nguyên số tiền đơn, chỉ ghi chú để người dùng tự điều chỉnh.
        var refunded = r.TryGetProperty("refunds", out var refunds) && refunds.ValueKind == JsonValueKind.Array
            ? refunds.EnumerateArray().Sum(x => Money(x, "amount"))
            : 0;
        if (refunded > 0)
        {
            var refundNote = $"Đơn đã được hoàn {refunded.ToString("0.00", CultureInfo.InvariantCulture)} {currency.ToUpperInvariant()}.";
            order.Note = order.Note is null ? refundNote : $"{order.Note} {refundNote}";
        }
        return order;
    }

    /// <summary>Quy đổi một đơn về USD (theo tỷ giá ngày đặt đơn) và tính các trường của khoản thu.</summary>
    private static EtsyOrder Build(string receiptId, DateOnly date, List<(string Title, int Qty)> items, string currency, FxService.FxRate fxRate,
        decimal itemTotal, decimal discount, decimal subtotal, decimal shipping, decimal tax, string country)
    {
        var rate = currency.Equals("EUR", StringComparison.OrdinalIgnoreCase) ? fxRate.EurToUsd : 1m;
        decimal Usd(decimal v) => decimal.Round(v * rate, 2, MidpointRounding.AwayFromZero);
        if (subtotal == 0) subtotal = itemTotal - discount;
        var amount = Usd(subtotal) + Usd(shipping);
        var title = items.Count == 0 ? $"Đơn Etsy #{receiptId}" : items[0].Title;
        if (title.Length > 180) title = title[..180] + "…";
        if (items.Count > 1) title += $" (+{items.Count - 1} sản phẩm)";
        var notes = new List<string>();
        if (rate != 1m)
            notes.Add(fxRate.Auto
                ? $"Quy đổi từ EUR theo tỷ giá ECB ngày {fxRate.Date}: 1 EUR = {rate.ToString(CultureInfo.InvariantCulture)} USD."
                : $"Quy đổi từ EUR theo tỷ giá dự phòng 1 EUR = {rate.ToString(CultureInfo.InvariantCulture)} USD (không lấy được tỷ giá tự động).");
        else if (!currency.Equals("USD", StringComparison.OrdinalIgnoreCase)) notes.Add($"Đơn bằng {currency}, chưa quy đổi.");
        return new EtsyOrder
        {
            ReceiptId = receiptId,
            Date = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Description = title,
            Quantity = Math.Max(1, items.Sum(i => i.Qty)),
            Currency = currency.ToUpperInvariant(),
            ItemTotal = Usd(itemTotal),
            Discount = Usd(discount),
            Subtotal = Usd(subtotal),
            Shipping = Usd(shipping),
            Tax = Usd(tax),
            Amount = amount,
            TaxPercent = amount == 0 ? 0 : decimal.Round(Usd(tax) / amount * 100, 2),
            Country = country.ToUpperInvariant(),
            SaleRegion = EuCountries.Contains(country.ToUpperInvariant()) ? "IN_EU" : "OUTSIDE_EU",
            Note = notes.Count > 0 ? string.Join(" ", notes) : null,
        };
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
