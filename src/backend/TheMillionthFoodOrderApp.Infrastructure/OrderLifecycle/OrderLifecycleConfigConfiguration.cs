using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Infrastructure.OrderLifecycle;

public sealed class OrderLifecycleConfigConfiguration : IEntityTypeConfiguration<OrderLifecycleConfig>
{
    public void Configure(EntityTypeBuilder<OrderLifecycleConfig> builder)
    {
        builder.ToTable("OrderLifecycleConfigs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ShopId)
            .IsRequired();

        // One config per shop
        builder.HasIndex(c => c.ShopId)
            .IsUnique();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasMany(c => c.Statuses)
            .WithOne()
            .HasForeignKey(s => s.OrderLifecycleConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Transitions)
            .WithOne()
            .HasForeignKey(t => t.OrderLifecycleConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(c => c.DomainEvents);
    }
}
