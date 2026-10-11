using Fin.Domain.Finance.Entities;
using Fin.Infrastructure.Finance.Contexts.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Finance.Contexts.Configurations
{
    public sealed class CategoryGroupConfiguration : IEntityTypeConfiguration<CategoryGroup>
    {
        public void Configure(EntityTypeBuilder<CategoryGroup> builder)
        {
            builder.ToTable("category_groups");

            builder.Property(x => x.Id).HasColumnName("id").HasConversion<ulong>().ValueGeneratedOnAdd();
            builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
            builder.Property(x => x.CategoryIds)
                .HasColumnName("category_ids")
                .HasColumnType("text")
                .HasConversion(IdListConverter.Converter, IdListConverter.Comparer)
                .IsRequired();
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("uq_group_name");
        }
    }
}
