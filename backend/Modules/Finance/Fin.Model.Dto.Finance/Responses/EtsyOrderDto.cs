namespace Fin.Model.Dto.Finance.Responses
{
    public sealed class EtsyOrderDto
    {
        public string ReceiptId { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Currency { get; set; } = "USD";
        public decimal ItemTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Tax { get; set; }
        public decimal Amount { get; set; }
        public decimal AmountAfterTax { get; set; }
        public decimal TaxPercent { get; set; }
        public string Country { get; set; } = string.Empty;
        public string SaleRegion { get; set; } = string.Empty;
        public string? Note { get; set; }
        public bool Imported { get; set; }
    }
}
