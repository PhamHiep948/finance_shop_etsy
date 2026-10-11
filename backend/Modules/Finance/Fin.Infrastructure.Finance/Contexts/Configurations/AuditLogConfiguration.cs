using Fin.Domain.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Finance.Contexts.Configurations
{
    public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
    {
        public void Configure(EntityTypeBuilder<AuditLog> builder)
        {
            builder.ToTable("audit_logs");

            builder.Property(x => x.Id).HasColumnName("id").HasConversion<ulong>().ValueGeneratedOnAdd();
            builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(20).IsRequired();
            builder.Property(x => x.EntityId).HasColumnName("entity_id").HasConversion<ulong>();
            builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(20).IsRequired();
            builder.Property(x => x.Label).HasColumnName("label").HasMaxLength(AuditLog.LabelMaxLength).IsRequired();
            builder.Property(x => x.Detail).HasColumnName("detail").HasMaxLength(500);
            builder.Property(x => x.Changes).HasColumnName("changes").HasColumnType("json");
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            builder.HasIndex(x => x.CreatedAt).HasDatabaseName("idx_audit_created");
            builder.HasIndex(x => new { x.EntityType, x.EntityId }).HasDatabaseName("idx_audit_entity");
        }
    }
}
