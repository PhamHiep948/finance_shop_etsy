using System.Globalization;
using Fin.Domain.Finance.Constants;
using Fin.Domain.Finance.Entities;
using Fin.Model.Dto.Finance.Requests;
using Fin.Service.Finance.Constants;

namespace Fin.Service.Finance.Services
{
    public static class LedgerEntryAssembler
    {
        public static void Apply(LedgerEntry entry, LedgerEntryRequest request)
        {
            entry.Date = DateOnly.ParseExact(request.Date, FinanceConstants.IsoDateFormat, CultureInfo.InvariantCulture);
            entry.Description = request.Description.Trim();
            entry.CategoryId = request.CategoryId;
            entry.CurrencyCode = SalesValues.DefaultCurrency;
            entry.OrderCode = request.OrderCode;
            entry.SaleRegion = request.SaleRegion;
            entry.SalesChannel = request.SalesChannel;
            entry.ProductQty = request.ProductQty;
            entry.Payee = request.Payee;
            entry.OriginScope = request.OriginScope;
            entry.PaymentMethod = request.PaymentMethod;
            entry.Details = new LedgerDetails
            {
                ReferenceCode = request.ReferenceCode,
                UnitPrice = request.UnitPrice,
                ItemTotal = request.ItemTotal,
                DiscountAmount = request.DiscountAmount,
                DiscountCode = request.DiscountCode,
                Subtotal = request.Subtotal,
                ShippingAmount = request.ShippingAmount,
                TaxAmount = request.TaxAmount,
                Note = request.Note,
                InvoiceId = request.InvoiceId,
            };
            entry.SetAmounts(request.Amount, request.TaxPercent);
        }
    }
}
