using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Fin.Infrastructure.Finance.Exceptions;
using Fin.Infrastructure.Finance.Interfaces;
using Fin.Infrastructure.Finance.Models;
using Fin.Infrastructure.Finance.Options;
using Fin.Infrastructure.Finance.Storage;
using Microsoft.Extensions.Options;

namespace Fin.Infrastructure.Finance.External
{
    public sealed class EtsyClient : IEtsyClient
    {
        private const string AuthorizeEndpoint = "https://www.etsy.com/oauth/connect";
        private const string TokenEndpoint = "https://api.etsy.com/v3/public/oauth/token";
        private const string ApiBase = "https://openapi.etsy.com/v3/application";
        private const string SettingsFile = "backend/Host/Fin.Host.Api/etsy.settings.json";
        private const int PageSize = 100;

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IOptionsMonitor<EtsyOptions> _options;
        private readonly FinanceStoragePaths _paths;
        private readonly TimeProvider _timeProvider;
        private readonly SemaphoreSlim _tokenLock = new(1, 1);
        // state -> code_verifier of PKCE flows that are waiting for the Etsy callback.
        private readonly Dictionary<string, string> _pendingAuthorizations = [];
        // A valid key is remembered; a rejected key is checked again after 30 seconds so a freshly fixed key is picked up.
        private (string Key, string? Error, DateTime CheckedAt)? _keyCheck;

        public EtsyClient(IHttpClientFactory httpClientFactory, IOptionsMonitor<EtsyOptions> options, FinanceStoragePaths paths, TimeProvider timeProvider)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _paths = paths;
            _timeProvider = timeProvider;
        }

        public bool IsReady => HasKeys && File.Exists(_paths.EtsyTokenFile);

        private EtsyOptions Settings => _options.CurrentValue;

        private bool HasKeys => !string.IsNullOrWhiteSpace(Settings.ApiKey) && !string.IsNullOrWhiteSpace(Settings.SharedSecret);

        private string ApiKeyHeader => $"{Settings.ApiKey.Trim()}:{Settings.SharedSecret.Trim()}";

        private DateTime UtcNow => _timeProvider.GetUtcNow().UtcDateTime;

        public async Task<EtsyConnectionStatus> GetStatusAsync(CancellationToken cancellationToken)
        {
            var keyError = HasKeys ? await CheckKeysAsync(cancellationToken) : null;
            var shopId = string.IsNullOrWhiteSpace(Settings.ShopId) ? ReadToken()?.ShopId ?? string.Empty : Settings.ShopId;
            return new EtsyConnectionStatus(HasKeys, keyError, File.Exists(_paths.EtsyTokenFile), shopId, Settings.RedirectUri);
        }

        public async Task<string> CreateAuthorizeUrlAsync(CancellationToken cancellationToken)
        {
            EnsureKeys();
            if (await CheckKeysAsync(cancellationToken) is { } keyError)
            {
                throw new EtsyException(keyError);
            }

            var verifier = Base64Url(RandomNumberGenerator.GetBytes(32));
            var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
            var state = Base64Url(RandomNumberGenerator.GetBytes(16));
            lock (_pendingAuthorizations)
            {
                _pendingAuthorizations[state] = verifier;
            }

            var query = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["redirect_uri"] = Settings.RedirectUri,
                ["scope"] = "transactions_r shops_r",
                ["client_id"] = Settings.ApiKey.Trim(),
                ["state"] = state,
                ["code_challenge"] = challenge,
                ["code_challenge_method"] = "S256",
            };
            return AuthorizeEndpoint + "?" + string.Join("&", query.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        }

        public async Task CompleteAuthorizationAsync(string code, string state, CancellationToken cancellationToken)
        {
            string? verifier;
            lock (_pendingAuthorizations)
            {
                _pendingAuthorizations.Remove(state, out verifier);
            }
            if (verifier is null)
            {
                throw new EtsyException("Phiên kết nối Etsy không hợp lệ hoặc đã hết hạn. Hãy bấm Kết nối lại.");
            }

            var token = await RequestTokenAsync(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["client_id"] = Settings.ApiKey.Trim(),
                ["redirect_uri"] = Settings.RedirectUri,
                ["code"] = code,
                ["code_verifier"] = verifier,
            }, null, cancellationToken);

            if (string.IsNullOrWhiteSpace(Settings.ShopId))
            {
                await DetectShopIdAsync(token, cancellationToken);
            }
        }

        public void Disconnect()
        {
            if (File.Exists(_paths.EtsyTokenFile))
            {
                File.Delete(_paths.EtsyTokenFile);
            }
        }

        public async Task<IReadOnlyList<EtsyReceipt>> GetPaidReceiptsAsync(DateOnly from, DateOnly to, CancellationToken cancellationToken)
        {
            EnsureKeys();
            var token = await GetTokenAsync(cancellationToken);
            var shopId = !string.IsNullOrWhiteSpace(Settings.ShopId) ? Settings.ShopId : token.ShopId ?? await DetectShopIdAsync(token, cancellationToken);
            using var client = CreateApiClient(token.AccessToken);
            var min = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue)).ToUnixTimeSeconds();
            var max = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue)).ToUnixTimeSeconds();
            var receipts = new List<EtsyReceipt>();

            for (var offset = 0; ; offset += PageSize)
            {
                var url = $"{ApiBase}/shops/{Uri.EscapeDataString(shopId)}/receipts?min_created={min}&max_created={max}&was_paid=true&was_canceled=false&limit={PageSize}&offset={offset}";
                using var response = await client.GetAsync(url, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    throw ReceiptsError(response.StatusCode, body, shopId);
                }

                using var doc = JsonDocument.Parse(body);
                var results = doc.RootElement.GetProperty("results");
                foreach (var receipt in results.EnumerateArray())
                {
                    // Canceled and fully refunded orders are no longer revenue.
                    var status = receipt.TryGetProperty("status", out var st) ? st.GetString()?.ToLowerInvariant() : null;
                    if (status is "canceled" or "fully refunded")
                    {
                        continue;
                    }
                    receipts.Add(MapReceipt(receipt));
                }
                if (results.GetArrayLength() < PageSize)
                {
                    break;
                }
            }

            return receipts;
        }

        private EtsyException ReceiptsError(HttpStatusCode status, string body, string shopId)
        {
            if (status == HttpStatusCode.Unauthorized)
            {
                Disconnect();
                return new EtsyException("Phiên đăng nhập Etsy đã hết hạn hoặc bị thu hồi. Hãy bấm \"Kết nối Etsy\" lại.");
            }
            if (status == HttpStatusCode.Forbidden)
            {
                return new EtsyException($"Etsy không cho đọc đơn của shop {shopId}: {EtsyError(body)}. "
                    + $"Kiểm tra đã đăng nhập đúng tài khoản chủ shop, và ShopId trong {SettingsFile} (nếu có điền) là đúng.");
            }
            return new EtsyException($"Etsy API lỗi ({(int)status}): {EtsyError(body)}");
        }

        private static EtsyReceipt MapReceipt(JsonElement receipt)
        {
            var currency = receipt.TryGetProperty("grandtotal", out var grandTotal) && grandTotal.TryGetProperty("currency_code", out var code)
                ? code.GetString() ?? "USD"
                : "USD";
            var items = receipt.TryGetProperty("transactions", out var transactions)
                ? transactions.EnumerateArray()
                    .Select(t => new EtsyReceiptItem(t.GetProperty("title").GetString() ?? string.Empty, t.TryGetProperty("quantity", out var q) ? q.GetInt32() : 1))
                    .ToList()
                : [];
            var created = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeSeconds(receipt.GetProperty("created_timestamp").GetInt64()).LocalDateTime);
            var refunded = receipt.TryGetProperty("refunds", out var refunds) && refunds.ValueKind == JsonValueKind.Array
                ? refunds.EnumerateArray().Sum(x => Money(x, "amount"))
                : 0;

            return new EtsyReceipt(
                receipt.GetProperty("receipt_id").GetInt64().ToString(CultureInfo.InvariantCulture),
                created,
                currency,
                items,
                Money(receipt, "total_price"),
                Money(receipt, "discount_amt"),
                Money(receipt, "subtotal"),
                Money(receipt, "total_shipping_cost"),
                Money(receipt, "total_tax_cost") + Money(receipt, "total_vat_cost"),
                receipt.TryGetProperty("country_iso", out var country) ? country.GetString() ?? string.Empty : string.Empty,
                refunded);
        }

        private static decimal Money(JsonElement parent, string name)
        {
            if (!parent.TryGetProperty(name, out var money) || money.ValueKind != JsonValueKind.Object)
            {
                return 0;
            }
            var divisor = money.GetProperty("divisor").GetDecimal();
            return divisor == 0 ? 0 : money.GetProperty("amount").GetDecimal() / divisor;
        }

        private void EnsureKeys()
        {
            if (!HasKeys)
            {
                throw new EtsyException($"Chưa điền ApiKey và SharedSecret trong {SettingsFile}.");
            }
        }

        private async Task<string?> CheckKeysAsync(CancellationToken cancellationToken)
        {
            var key = ApiKeyHeader;
            if (_keyCheck is { } cached && cached.Key == key && (cached.Error is null || UtcNow - cached.CheckedAt < TimeSpan.FromSeconds(30)))
            {
                return cached.Error;
            }

            string? error = null;
            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("x-api-key", key);
                using var response = await client.GetAsync($"{ApiBase}/openapi-ping", cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    error = (int)response.StatusCode is 401 or 403
                        ? "Etsy từ chối ApiKey / SharedSecret. Kiểm tra: (1) app trên etsy.com/developers/your-apps đã được duyệt (không còn Pending), "
                          + "(2) copy lại đúng Keystring và Shared Secret. Etsy trả lời: " + EtsyError(body)
                        : $"Etsy API lỗi ({(int)response.StatusCode}): {EtsyError(body)}";
                }
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
            {
                // Offline: do not conclude that the key is wrong, and do not cache the result.
                return "Không kết nối được tới Etsy (kiểm tra Internet).";
            }

            _keyCheck = (key, error, UtcNow);
            return error;
        }

        private sealed record TokenData(string AccessToken, string RefreshToken, DateTime ExpiresAt, string? ShopId = null);

        private TokenData? ReadToken()
        {
            try
            {
                return File.Exists(_paths.EtsyTokenFile) ? JsonSerializer.Deserialize<TokenData>(File.ReadAllText(_paths.EtsyTokenFile)) : null;
            }
            catch (Exception e) when (e is IOException or JsonException)
            {
                return null;
            }
        }

        private async Task<TokenData> RequestTokenAsync(Dictionary<string, string> form, string? shopId, CancellationToken cancellationToken)
        {
            using var client = _httpClientFactory.CreateClient();
            using var content = new FormUrlEncodedContent(form);
            using var response = await client.PostAsync(TokenEndpoint, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                // An expired or revoked refresh token cannot be recovered; dropping it makes the UI ask to connect again.
                if (form["grant_type"] == "refresh_token")
                {
                    Disconnect();
                }
                throw new EtsyException($"Etsy từ chối cấp token ({(int)response.StatusCode}): {EtsyError(body)}. Hãy bấm \"Kết nối Etsy\" lại.");
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var token = new TokenData(
                root.GetProperty("access_token").GetString()!,
                root.GetProperty("refresh_token").GetString()!,
                UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32() - 60),
                shopId);
            await File.WriteAllTextAsync(_paths.EtsyTokenFile, JsonSerializer.Serialize(token), cancellationToken);
            return token;
        }

        private async Task<TokenData> GetTokenAsync(CancellationToken cancellationToken)
        {
            await _tokenLock.WaitAsync(cancellationToken);
            try
            {
                if (!File.Exists(_paths.EtsyTokenFile))
                {
                    throw new EtsyException("Chưa kết nối tài khoản Etsy. Bấm \"Kết nối Etsy\" để đăng nhập.");
                }
                var token = JsonSerializer.Deserialize<TokenData>(await File.ReadAllTextAsync(_paths.EtsyTokenFile, cancellationToken))
                    ?? throw new EtsyException("File token Etsy bị hỏng, hãy kết nối lại.");
                if (token.ExpiresAt > UtcNow)
                {
                    return token;
                }

                return await RequestTokenAsync(new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["client_id"] = Settings.ApiKey.Trim(),
                    ["refresh_token"] = token.RefreshToken,
                }, token.ShopId, cancellationToken);
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        private HttpClient CreateApiClient(string accessToken)
        {
            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("x-api-key", ApiKeyHeader);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return client;
        }

        private async Task<string> DetectShopIdAsync(TokenData token, CancellationToken cancellationToken)
        {
            using var client = CreateApiClient(token.AccessToken);
            using var response = await client.GetAsync($"{ApiBase}/users/me", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new EtsyException($"Không lấy được shop của tài khoản Etsy ({(int)response.StatusCode}): {EtsyError(body)}");
            }

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("shop_id", out var id) || id.ValueKind != JsonValueKind.Number)
            {
                throw new EtsyException("Tài khoản Etsy này chưa có shop. Hãy đăng nhập đúng tài khoản chủ shop.");
            }

            var shopId = id.GetInt64().ToString(CultureInfo.InvariantCulture);
            await _tokenLock.WaitAsync(cancellationToken);
            try
            {
                var saved = ReadToken() ?? token;
                await File.WriteAllTextAsync(_paths.EtsyTokenFile, JsonSerializer.Serialize(saved with { ShopId = shopId }), cancellationToken);
            }
            finally
            {
                _tokenLock.Release();
            }
            return shopId;
        }

        private static string EtsyError(string body)
        {
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString()!;
                }
            }
            catch (JsonException)
            {
                // Not JSON (HTML error page, proxy message): fall through and show the raw text.
            }
            return body.Length > 300 ? body[..300] : body;
        }

        private static string Base64Url(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
