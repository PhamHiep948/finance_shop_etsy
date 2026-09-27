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
    /// <summary>true = dùng đơn hàng giả khi CHƯA điền key. Điền đủ ApiKey/SharedSecret thì tự dùng dữ liệu thật.</summary>
    public bool UseMockData { get; set; } = true;
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

    private EtsyOptions O => options.CurrentValue;
    private string TokenFile => Path.Combine(env.ContentRootPath, "etsy.tokens.json");
    private bool HasKeys => !string.IsNullOrWhiteSpace(O.ApiKey) && !string.IsNullOrWhiteSpace(O.SharedSecret);
    // Đã có key thì luôn gọi Etsy thật; dữ liệu giả chỉ dùng khi chưa cấu hình.
    private bool Mock => O.UseMockData && !HasKeys;

    public object Status() => new
    {
        mock = Mock,
        configured = HasKeys,
        connected = File.Exists(TokenFile),
        shopId = string.IsNullOrWhiteSpace(O.ShopId) ? ReadToken()?.ShopId ?? "" : O.ShopId,
        redirectUri = O.RedirectUri,
        ready = Mock || (HasKeys && File.Exists(TokenFile)),
    };

    // ---------------------------------------------------------------- OAuth (PKCE)

    public string AuthorizeUrl()
    {
        if (!HasKeys) throw new EtsyException("Chưa điền ApiKey và SharedSecret trong backend/etsy.settings.json.");
        var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));
        lock (pending) pending[state] = verifier;
        var query = new Dictionary<string, string>
        {
            ["response_type"] = "code",
            ["redirect_uri"] = O.RedirectUri,
            ["scope"] = "transactions_r shops_r",
            ["client_id"] = O.ApiKey,
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
            ["client_id"] = O.ApiKey,
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
        if (!res.IsSuccessStatusCode) throw new EtsyException($"Etsy từ chối cấp token ({(int)res.StatusCode}): {body}");
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
                ["client_id"] = O.ApiKey,
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
        client.DefaultRequestHeaders.Add("x-api-key", $"{O.ApiKey}:{O.SharedSecret}");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>Hỏi Etsy shop của tài khoản đang đăng nhập (GET /users/me) rồi lưu vào file token.</summary>
    private async Task<string> DetectShopIdAsync(TokenData token)
    {
        using var client = ApiClient(token.AccessToken);
        using var res = await client.GetAsync($"{ApiBase}/users/me");
        var body = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode) throw new EtsyException($"Không lấy được shop của tài khoản Etsy ({(int)res.StatusCode}): {body}");
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
        return Mock ? MockOrders(from, to, rates) : await RealOrdersAsync(from, to, rates);
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
            var url = $"{ApiBase}/shops/{Uri.EscapeDataString(shopId)}/receipts?min_created={min}&max_created={max}&was_paid=true&limit=100&offset={offset}";
            using var res = await client.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode) throw new EtsyException($"Etsy API lỗi ({(int)res.StatusCode}): {body}");
            using var doc = JsonDocument.Parse(body);
            var results = doc.RootElement.GetProperty("results");
            foreach (var receipt in results.EnumerateArray()) orders.Add(MapReceipt(receipt, rates));
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
        return Build(
            r.GetProperty("receipt_id").GetInt64().ToString(CultureInfo.InvariantCulture),
            created, titles, currency, rates(created),
            Money(r, "total_price"), Money(r, "discount_amt"), Money(r, "subtotal"),
            Money(r, "total_shipping_cost"), Money(r, "total_tax_cost") + Money(r, "total_vat_cost"),
            r.TryGetProperty("country_iso", out var country) ? country.GetString() ?? "" : "");
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

    /// <summary>Đơn giả, cố định theo ngày (gọi lại cùng khoảng ngày sẽ ra đúng các đơn cũ).</summary>
    private static List<EtsyOrder> MockOrders(DateOnly from, DateOnly to, Func<DateOnly, FxService.FxRate> rates)
    {
        string[] products =
        [
            "Lily Flower Crochet Bouquet", "Tulip Crochet Keychain", "Sunflower Hair Clip", "Mini Amigurumi Bear",
            "Rose Crochet Bouquet (5 stems)", "Daisy Tote Bag", "Lavender Crochet Pot", "Crochet Cat Plush",
        ];
        decimal[] prices = [12.9m, 5.49m, 6.5m, 18m, 24.9m, 29m, 15.5m, 21m];
        string[] countries = ["US", "DE", "FR", "US", "GB", "NL", "CA", "IT", "AU", "ES"];
        var orders = new List<EtsyOrder>();
        for (var d = from; d <= to && d <= DateOnly.FromDateTime(DateTime.Today); d = d.AddDays(1))
        {
            var rnd = new Random(d.DayNumber);
            var count = rnd.Next(0, 3);
            for (var i = 0; i < count; i++)
            {
                var p = rnd.Next(products.Length);
                var qty = rnd.NextDouble() < 0.75 ? 1 : rnd.Next(2, 4);
                var extra = rnd.NextDouble() < 0.2 ? rnd.Next(products.Length) : -1;
                var items = new List<(string, int)> { (products[p], qty) };
                var itemTotal = prices[p] * qty;
                if (extra >= 0) { items.Add((products[extra], 1)); itemTotal += prices[extra]; }
                var country = countries[rnd.Next(countries.Length)];
                var eu = EuCountries.Contains(country);
                var currency = eu && rnd.NextDouble() < 0.6 ? "EUR" : "USD";
                if (currency == "EUR") itemTotal = decimal.Round(itemTotal * 0.92m, 2);
                var discount = rnd.NextDouble() < 0.2 ? decimal.Round(itemTotal * 0.1m, 2) : 0;
                var shipping = new[] { 0m, 3.5m, 4.9m, 6.9m }[rnd.Next(4)];
                var subtotal = itemTotal - discount;
                var tax = eu ? decimal.Round((subtotal + shipping) * 0.19m, 2) : 0;
                orders.Add(Build((5_100_000_000L + d.DayNumber * 10L + i).ToString(CultureInfo.InvariantCulture),
                    d, items, currency, rates(d), itemTotal, discount, subtotal, shipping, tax, country));
            }
        }
        return orders;
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
