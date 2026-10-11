using Fin.Model.Dto.Finance.Responses;
using Fin.Service.Finance.Services;

namespace Fin.Tests.Unit.Services
{
    public sealed class AuditTrailTests
    {
        [Fact]
        public void Numbers_with_different_formatting_are_not_a_change()
        {
            var before = new LedgerEntryResponse { Amount = 29.00m, Description = "Mug", Note = "" };
            var after = new LedgerEntryResponse { Amount = 29m, Description = "Mug", Note = null };

            Assert.Empty(AuditTrail.Diff(before, after));
        }

        [Fact]
        public void Changed_fields_are_reported_with_old_and_new_text()
        {
            var before = new LedgerEntryResponse { Amount = 10m, Description = "Mug" };
            var after = new LedgerEntryResponse { Amount = 12.5m, Description = "Mug lớn" };

            var changes = AuditTrail.Diff(before, after);

            Assert.Equal(new AuditChange("10", "12.5"), changes["amount"]);
            Assert.Equal(new AuditChange("Mug", "Mug lớn"), changes["description"]);
        }

        [Fact]
        public void Creation_reports_every_filled_field()
        {
            var changes = AuditTrail.Diff(null, new LedgerEntryResponse { Date = "2026-10-05", Description = "Mug", CategoryId = 1 });

            Assert.True(changes.ContainsKey("date"));
            Assert.True(changes.ContainsKey("categoryId"));
            Assert.False(changes.ContainsKey("note"));
        }
    }
}
