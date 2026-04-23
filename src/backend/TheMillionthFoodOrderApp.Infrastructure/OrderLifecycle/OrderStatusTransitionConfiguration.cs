using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Infrastructure.OrderLifecycle;

public sealed class OrderStatusTransitionConfiguration : IEntityTypeConfiguration<OrderStatusTransition>
{
    public void Configure(EntityTypeBuilder<OrderStatusTransition> builder)
    {
        builder.ToTable("OrderStatusTransitions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.OrderLifecycleConfigId)
            .IsRequired();

        builder.Property(t => t.FromStatusId)
            .IsRequired();

        builder.Property(t => t.ToStatusId)
            .IsRequired();

        // Use Restrict to avoid SQL Server multiple cascade path error —
        // parent cascade from OrderLifecycleConfig handles cleanup.
        builder.HasOne<OrderStatus>()
            .WithMany()
            .HasForeignKey(t => t.FromStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<OrderStatus>()
            .WithMany()
            .HasForeignKey(t => t.ToStatusId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.OrderLifecycleConfigId, t.FromStatusId, t.ToStatusId })
            .IsUnique()
            .HasDatabaseName("IX_OrderStatusTransitions_ConfigId_From_To");

        builder.Ignore(t => t.DomainEvents);
    }
}
