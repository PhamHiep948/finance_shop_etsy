namespace Fin.Infrastructure.Finance.Options
{
    public sealed class FinanceStorageOptions
    {
        public const string SectionName = "Storage";

        public string UploadsFolder { get; set; } = "uploads";
        public string EtsyTokenFile { get; set; } = "etsy.tokens.json";
        public string EtsyAutoSyncFile { get; set; } = "etsy.autosync.json";
    }
}
