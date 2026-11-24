using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ShipmentService.Shipments;

namespace ShipmentService.Persistence.Configurations;

public sealed class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("shipments");

        builder.HasKey(shipment => shipment.Id);

        builder.Property(shipment => shipment.Id)
            .HasColumnName("id");

        builder.Property(shipment => shipment.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.HasIndex(shipment => shipment.OrderId)
            .IsUnique()
            .HasDatabaseName("ix_shipments_order_id");

        builder.Property(shipment => shipment.MessageId)
            .HasColumnName("message_id")
            .IsRequired();

        builder.HasIndex(shipment => shipment.MessageId)
            .IsUnique()
            .HasDatabaseName("ix_shipments_message_id");

        builder.Property(shipment => shipment.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(shipment => shipment.CreatedAtUtc)
            .HasColumnName("created_at_utc");
    }
}
