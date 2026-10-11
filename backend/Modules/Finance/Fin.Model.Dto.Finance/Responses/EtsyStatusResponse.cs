namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class EtsyStatusResponse
    {
        public bool Configured { get; set; }
        public string? KeyError { get; set; }
        public bool Connected { get; set; }
        public string ShopId { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public bool Ready { get; set; }
    }
}
