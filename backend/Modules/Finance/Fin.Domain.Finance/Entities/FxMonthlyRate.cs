namespace Fin.Domain.Finance.Entities
{
    public sealed class FxMonthlyRate
    {
        public const decimal MinEurToUsd = 0.5m;
        public const decimal MaxEurToUsd = 3m;

        public string Month { get; set; } = string.Empty;
        public decimal EurToUsd { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
