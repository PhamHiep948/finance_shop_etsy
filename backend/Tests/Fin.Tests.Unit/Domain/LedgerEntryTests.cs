using Fin.Domain.Finance.Entities;

namespace Fin.Tests.Unit.Domain
{
    public sealed class LedgerEntryTests
    {
        [Fact]
        public void Manual_amounts_derive_the_post_tax_value()
        {
            var entry = new LedgerEntry();

            entry.SetAmounts(20m, 10m);

            Assert.Equal(22m, entry.AmountAfterTax);
        }

        [Fact]
        public void A_known_paid_amount_is_kept_exactly()
        {
            var entry = new LedgerEntry();

            entry.SetAmounts(LedgerEntry.BeforeTax(5.79m, 19m), 19m, 5.79m);

            Assert.Equal(4.87m, entry.Amount);
            Assert.Equal(5.79m, entry.AmountAfterTax);
        }

        [Fact]
        public void Soft_delete_and_restore_toggle_the_trash_state()
        {
            var entry = new LedgerEntry();

            entry.SoftDelete(new DateTime(2026, 10, 5));
            Assert.True(entry.IsDeleted);

            entry.Restore();
            Assert.False(entry.IsDeleted);
        }
    }
}
