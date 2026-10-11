using Fin.Domain.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Finance.Contexts.Configurations
{
    public sealed class FxMonthlyRateConfiguration : IEntityTypeConfiguration<FxMonthlyRate>
    {
        public void Configure(EntityTypeBuilder<FxMonthlyRate> builder)
        {
            builder.ToTable("fx_monthly");

            builder.HasKey(x => x.Month);
            builder.Property(x => x.Month).HasColumnName("month").HasColumnType("char(7)");
            builder.Property(x => x.EurToUsd).HasColumnName("eur_to_usd").HasPrecision(12, 6);
            builder.Property(x => x.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAddOrUpdate();
        }
    }
}
