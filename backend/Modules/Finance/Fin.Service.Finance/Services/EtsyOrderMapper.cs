using System.Globalization;
using Fin.Domain.Finance.Constants;
using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Models;
using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Constants;

namespace Fin.Service.Finance.Services
{
    public static class EtsyOrderMapper
    {
        private const int TitleMaxLength = 180;

        private static readonly HashSet<string> EuCountries =
        [
            "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR", "DE", "GR", "HU", "IE",
            "IT", "LV", "LT", "LU", "MT", "NL", "PL", "PT", "RO", "SK", "SI", "ES", "SE",
        ];

        public static EtsyOrderDto Map(EtsyReceipt receipt, IReadOnlyDictionary<string, decimal> monthlyRates, decimal euVatPercent)
        {
            var isEur = receipt.Currency.Equals("EUR", StringComparison.OrdinalIgnoreCase);
            var monthKey = receipt.Created.ToString(FinanceConstants.MonthFormat, CultureInfo.InvariantCulture);
            var monthLabel = receipt.Created.ToString("MM/yyyy", CultureInfo.InvariantCulture);
            // A month without a typed rate (not published yet) converts at 1 EUR = 1 USD until the user enters it.
            var hasManualRate = monthlyRates.TryGetValue(monthKey, out var manualRate) && manualRate > 0;
            var rate = isEur && hasManualRate ? manualRate : 1m;
            decimal Usd(decimal value) => decimal.Round(value * rate, 2, MidpointRounding.AwayFromZero);

            var subtotal = receipt.Subtotal == 0 ? receipt.TotalPrice - receipt.Discount : receipt.Subtotal;
            var received = Usd(subtotal) + Usd(receipt.Shipping);
            var isEu = EuCountries.Contains(receipt.Country.ToUpperInvariant());

            decimal amount, afterTax, taxPercent, taxUsd;
            if (isEu)
            {
                // Inside the EU Etsy collects the VAT, so the money received is already the post-tax amount.
                taxPercent = euVatPercent;
                afterTax = received;
                amount = LedgerEntry.BeforeTax(received, taxPercent);
                taxUsd = afterTax - amount;
            }
            else
            {
                amount = received;
                taxUsd = Usd(receipt.Tax);
                taxPercent = amount == 0 ? 0 : decimal.Round(taxUsd / amount * 100, 2);
                afterTax = LedgerEntry.AfterTax(amount, taxPercent);
            }

            var notes = new List<string>();
            if (isEur && hasManualRate)
            {
                notes.Add($"Quy đổi từ EUR theo tỷ giá tự nhập tháng {monthLabel}: 1 EUR = {rate.ToString(CultureInfo.InvariantCulture)} USD.");
            }
            else if (isEur)
            {
                notes.Add($"Chưa nhập tỷ giá tháng {monthLabel}: tạm tính mặc định 1 EUR = 1 USD.");
            }
            else if (!receipt.Currency.Equals("USD", StringComparison.OrdinalIgnoreCase))
            {
                notes.Add($"Đơn bằng {receipt.Currency}, chưa quy đổi.");
            }
            if (isEu)
            {
                notes.Add($"Trong EU: Etsy đã thu VAT {taxPercent.ToString("0.##", CultureInfo.InvariantCulture)}%, tiền nhận về ghi vào Sau thuế.");
            }
            if (receipt.Refunded > 0)
            {
                // A partial refund keeps the order amount; the note lets the user adjust it by hand.
                notes.Add($"Đơn đã được hoàn {receipt.Refunded.ToString("0.00", CultureInfo.InvariantCulture)} {receipt.Currency.ToUpperInvariant()}.");
            }

            return new EtsyOrderDto
            {
                ReceiptId = receipt.ReceiptId,
                Date = receipt.Created.ToString(FinanceConstants.IsoDateFormat, CultureInfo.InvariantCulture),
                Description = Title(receipt),
                Quantity = Math.Max(1, receipt.Items.Sum(i => i.Quantity)),
                Currency = receipt.Currency.ToUpperInvariant(),
                ItemTotal = Usd(receipt.TotalPrice),
                Discount = Usd(receipt.Discount),
                Subtotal = Usd(subtotal),
                Shipping = Usd(receipt.Shipping),
                Tax = taxUsd,
                Amount = amount,
                AmountAfterTax = afterTax,
                TaxPercent = taxPercent,
                Country = receipt.Country.ToUpperInvariant(),
                SaleRegion = isEu ? SalesValues.RegionInEu : SalesValues.RegionOutsideEu,
                Note = notes.Count > 0 ? string.Join(" ", notes) : null,
            };
        }

        private static string Title(EtsyReceipt receipt)
        {
            if (receipt.Items.Count == 0)
            {
                return $"Đơn Etsy #{receipt.ReceiptId}";
            }

            var title = receipt.Items[0].Title;
            if (title.Length > TitleMaxLength)
            {
                title = title[..TitleMaxLength] + "…";
            }
            return receipt.Items.Count > 1 ? $"{title} (+{receipt.Items.Count - 1} sản phẩm)" : title;
        }
    }
}
