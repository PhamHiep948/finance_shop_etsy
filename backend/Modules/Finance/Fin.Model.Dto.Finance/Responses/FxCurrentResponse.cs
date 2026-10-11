namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class FxCurrentResponse
    {
        public decimal EurToUsd { get; set; }
        public decimal UsdToEur { get; set; }
        public string Date { get; set; } = string.Empty;
        public bool Auto { get; set; }
        public bool Manual { get; set; }
    }
}
