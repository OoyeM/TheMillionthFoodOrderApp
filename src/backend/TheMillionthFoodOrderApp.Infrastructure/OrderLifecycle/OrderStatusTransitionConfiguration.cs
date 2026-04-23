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

        // ClientCascade: EF Core cascades deletions client-side without a DB-level CASCADE,
        // avoiding SQL Server's multiple cascade path error while still cleaning up transitions
        // when their parent statuses are deleted.
        builder.HasOne<OrderStatus>()
            .WithMany()
            .HasForeignKey(t => t.FromStatusId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasOne<OrderStatus>()
            .WithMany()
            .HasForeignKey(t => t.ToStatusId)
            .OnDelete(DeleteBehavior.ClientCascade);

        builder.HasIndex(t => new { t.OrderLifecycleConfigId, t.FromStatusId, t.ToStatusId })
            .IsUnique()
            .HasDatabaseName("IX_OrderStatusTransitions_ConfigId_From_To");

        builder.Ignore(t => t.DomainEvents);
    }
}
