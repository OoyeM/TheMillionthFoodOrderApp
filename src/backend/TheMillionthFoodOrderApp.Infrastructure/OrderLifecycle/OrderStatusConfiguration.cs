using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Infrastructure.OrderLifecycle;

public sealed class OrderStatusConfiguration : IEntityTypeConfiguration<OrderStatus>
{
    public void Configure(EntityTypeBuilder<OrderStatus> builder)
    {
        builder.ToTable("OrderStatuses");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.OrderLifecycleConfigId)
            .IsRequired();

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.SystemKey)
            .HasMaxLength(50);

        builder.Property(s => s.SortOrder)
            .IsRequired();

        builder.Property(s => s.IsEnabled)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(s => s.IsTerminal)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.ColorHex)
            .HasMaxLength(7);

        builder.HasIndex(s => new { s.OrderLifecycleConfigId, s.SortOrder })
            .IsUnique()
            .HasDatabaseName("IX_OrderStatuses_ConfigId_SortOrder");

        builder.Ignore(s => s.DomainEvents);
    }
}
