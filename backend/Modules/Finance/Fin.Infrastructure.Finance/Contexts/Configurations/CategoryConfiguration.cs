using Fin.Domain.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Finance.Contexts.Configurations
{
    public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("categories");

            // Ids are BIGINT UNSIGNED in the legacy schema; the entity keeps a signed long for simplicity.
            builder.Property(x => x.Id).HasColumnName("id").HasConversion<ulong>().ValueGeneratedOnAdd();
            builder.Property(x => x.Kind).HasColumnName("kind").HasMaxLength(10).IsRequired();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();

            builder.HasIndex(x => new { x.Kind, x.Name }).IsUnique().HasDatabaseName("uq_kind_name");
        }
    }
}
