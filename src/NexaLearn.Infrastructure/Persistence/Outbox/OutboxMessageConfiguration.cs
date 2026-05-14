using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NexaLearn.Infrastructure.Persistence.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id");
        builder.Property(o => o.Type).HasColumnName("type").HasMaxLength(500).IsRequired();
        builder.Property(o => o.Content).HasColumnName("content").IsRequired();
        builder.Property(o => o.OccurredAt).HasColumnName("occurred_at").IsRequired();
        builder.Property(o => o.ProcessedAt).HasColumnName("processed_at");

        builder.HasIndex(o => o.ProcessedAt).HasDatabaseName("ix_outbox_messages_processed_at");
    }
}
