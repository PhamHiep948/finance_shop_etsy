using Fin.Infrastructure.Finance.Contexts.Converters;

namespace Fin.Tests.Unit.Infrastructure
{
    public sealed class LedgerDetailsConverterTests
    {
        [Fact]
        public void Reads_legacy_json_with_numbers_as_text_and_unknown_keys()
        {
            var details = LedgerDetailsConverter.Deserialize("""{"unitPrice":"5.49","itemTotal":10,"linkedIncomeId":"7","note":"x"}""");

            Assert.Equal(5.49m, details.UnitPrice);
            Assert.Equal(10m, details.ItemTotal);
            Assert.Equal("x", details.Note);
        }

        [Fact]
        public void Broken_or_empty_json_becomes_empty_details()
        {
            Assert.Null(LedgerDetailsConverter.Deserialize("{not json").Note);
            Assert.Null(LedgerDetailsConverter.Deserialize(null).Note);
            Assert.Null(LedgerDetailsConverter.Deserialize("""{"unitPrice":""}""").UnitPrice);
        }
    }
}
