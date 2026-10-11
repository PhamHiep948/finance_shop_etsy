namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class FxMonthRowResponse
    {
        public string Month { get; set; } = string.Empty;
        public decimal? Manual { get; set; }
        public decimal? Ecb { get; set; }
    }
}
