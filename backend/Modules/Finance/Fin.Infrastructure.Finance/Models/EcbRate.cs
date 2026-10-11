namespace Fin.Infrastructure.Finance.Models
{
    public sealed record EcbRate(decimal EurToUsd, string Date, bool Auto);
}
