using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class LedgerInput
{
    public string Date { get; set; } = "";
    public string Description { get; set; } = "";
    public long CategoryId { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal AmountAfterTax { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? OrderCode { get; set; }
    public string? SaleRegion { get; set; }
    public string? SalesChannel { get; set; }
    public int? ProductQty { get; set; }
    public string? Payee { get; set; }
    public string? OriginScope { get; set; }
    public string? PaymentMethod { get; set; }
    [JsonExtensionData] public Dictionary<string, JsonElement> Extra { get; set; } = [];
}

public sealed class CategoryInput
{
    public string? Name { get; set; }
}

public sealed record AttachmentInfo(long Id, long EntryId, string OriginalName, string ContentType, long Size, DateTime CreatedAt)
{
    [JsonIgnore] public string Path { get; init; } = "";
}
