using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id)
            .HasColumnName("id");

        builder.Property(order => order.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(order => order.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(order => order.CustomerName)
            .HasColumnName("customer_name")
            .HasMaxLength(Order.CustomerNameMaxLength)
            .IsRequired();

        builder.Property(order => order.Amount)
            .HasColumnName("amount")
            .HasPrecision(18, 2);

        builder.Property(order => order.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(OrderStatus.Draft)
            .IsRequired();

        builder.Property(order => order.Fulfillment)
            .HasColumnName("fulfillment")
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(order => order.CreatedAtUtc)
            .HasColumnName("created_at_utc");
    }
}
