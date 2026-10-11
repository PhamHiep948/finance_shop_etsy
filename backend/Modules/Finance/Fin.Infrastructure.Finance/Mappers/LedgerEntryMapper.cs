using System.Globalization;
using Fin.Domain.Finance.Entities;
using Fin.Model.Dto.Finance.Responses;

namespace Fin.Infrastructure.Finance.Mappers
{
    public static class LedgerEntryMapper
    {
        public static LedgerEntryResponse ToResponse(LedgerEntry entry, int attachmentCount) => new()
        {
            Id = entry.Id,
            Kind = entry.Kind,
            Date = entry.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Description = entry.Description,
            CategoryId = entry.CategoryId,
            Amount = entry.Amount,
            TaxPercent = entry.TaxPercent,
            AmountAfterTax = entry.AmountAfterTax,
            CurrencyCode = entry.CurrencyCode,
            OrderCode = entry.OrderCode,
            SaleRegion = entry.SaleRegion,
            SalesChannel = entry.SalesChannel,
            ProductQty = entry.ProductQty,
            Payee = entry.Payee,
            OriginScope = entry.OriginScope,
            PaymentMethod = entry.PaymentMethod,
            Source = entry.Source,
            LinkedIncomeId = entry.LinkedIncomeId,
            ReferenceCode = entry.Details.ReferenceCode,
            UnitPrice = entry.Details.UnitPrice,
            ItemTotal = entry.Details.ItemTotal,
            DiscountAmount = entry.Details.DiscountAmount,
            DiscountCode = entry.Details.DiscountCode,
            Subtotal = entry.Details.Subtotal,
            ShippingAmount = entry.Details.ShippingAmount,
            TaxAmount = entry.Details.TaxAmount,
            Note = entry.Details.Note,
            InvoiceId = entry.Details.InvoiceId,
            AttachmentCount = attachmentCount,
            CreatedAt = entry.CreatedAt,
            UpdatedAt = entry.UpdatedAt,
            DeletedAt = entry.DeletedAt,
        };
    }
}
