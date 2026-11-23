using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id");

        builder.Property(user => user.Username)
            .HasColumnName("username")
            .HasMaxLength(User.UsernameMaxLength)
            .IsRequired();

        builder.Property(user => user.NormalizedUsername)
            .HasColumnName("normalized_username")
            .HasMaxLength(User.UsernameMaxLength)
            .IsRequired();

        builder.HasIndex(user => user.NormalizedUsername)
            .IsUnique()
            .HasDatabaseName("ix_users_normalized_username");

        builder.Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(User.PasswordHashMaxLength)
            .IsRequired();

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnName("created_at_utc");
    }
}
