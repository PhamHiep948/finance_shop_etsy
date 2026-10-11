namespace Fin.Infrastructure.Finance.Models
{
    public sealed record EtsyReceipt(
        string ReceiptId,
        DateOnly Created,
        string Currency,
        IReadOnlyList<EtsyReceiptItem> Items,
        decimal TotalPrice,
        decimal Discount,
        decimal Subtotal,
        decimal Shipping,
        decimal Tax,
        string Country,
        decimal Refunded);
}
