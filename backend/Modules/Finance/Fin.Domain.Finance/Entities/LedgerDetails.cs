namespace Fin.Domain.Finance.Entities
{
    public sealed class LedgerDetails
    {
        public string? ReferenceCode { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? ItemTotal { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? DiscountCode { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? ShippingAmount { get; set; }
        public decimal? TaxAmount { get; set; }
        public string? Note { get; set; }
        public string? InvoiceId { get; set; }
    }
}
