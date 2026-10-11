using Fin.Domain.Finance.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Finance.Contexts.Configurations
{
    public sealed class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
    {
        public void Configure(EntityTypeBuilder<Attachment> builder)
        {
            builder.ToTable("attachments");

            builder.Property(x => x.Id).HasColumnName("id").HasConversion<ulong>().ValueGeneratedOnAdd();
            builder.Property(x => x.EntryId).HasColumnName("entry_id").HasConversion<ulong>();
            builder.Property(x => x.OriginalName).HasColumnName("original_name").HasMaxLength(255).IsRequired();
            builder.Property(x => x.StoredName).HasColumnName("stored_name").HasMaxLength(100).IsRequired();
            builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(150).IsRequired();
            builder.Property(x => x.SizeBytes).HasColumnName("size_bytes");
            builder.Property(x => x.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .ValueGeneratedOnAdd();

            builder.HasIndex(x => x.EntryId).HasDatabaseName("idx_attachment_entry");

            // Purging an entry from the trash removes its attachment rows in the database as well.
            builder.HasOne(x => x.Entry)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.EntryId)
                .HasConstraintName("fk_attachment_entry")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
