namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class FxMonthlyResponse
    {
        public string Month { get; set; } = string.Empty;
        public decimal EurToUsd { get; set; }
    }
}
