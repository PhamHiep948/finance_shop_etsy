namespace Fin.Infrastructure.Finance.Options
{
    public sealed class EtsyOptions
    {
        public const string SectionName = "Etsy";

        public string ApiKey { get; set; } = string.Empty;
        public string SharedSecret { get; set; } = string.Empty;
        public string ShopId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = "http://localhost:5000/api/v1/etsy/callback";
        public long DefaultCategoryId { get; set; } = 1;
        public decimal EurToUsd { get; set; } = 1.087m;
        public decimal EuVatPercent { get; set; } = 19m;
    }
}
