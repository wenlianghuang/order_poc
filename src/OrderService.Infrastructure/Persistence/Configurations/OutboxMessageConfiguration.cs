using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id");

        builder.Property(message => message.Type)
            .HasColumnName("type")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(message => message.CreatedAtUtc)
            .HasColumnName("created_at_utc");

        builder.Property(message => message.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        builder.HasIndex(message => message.CreatedAtUtc)
            .HasDatabaseName("ix_outbox_messages_pending")
            .HasFilter("processed_at_utc IS NULL");
    }
}
