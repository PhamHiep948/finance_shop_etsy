namespace Fin.Infrastructure.Finance.Models
{
    public sealed record EtsyConnectionStatus(bool Configured, string? KeyError, bool Connected, string ShopId, string RedirectUri)
    {
        public bool Ready => Configured && KeyError is null && Connected;
    }
}
