using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Infrastructure.Orders;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrderId)
            .IsRequired();

        builder.Property(i => i.ProductId)
            .IsRequired();

        builder.Property(i => i.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.Quantity)
            .IsRequired();

        builder.Property(i => i.UnitGrossPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.UnitNetPrice)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.UnitVatAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.LineTotal)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        // SelectedModifiers stored as an owned entity collection in a separate table.
        builder.OwnsMany(i => i.SelectedModifiers, sm =>
        {
            sm.ToTable("OrderItemSelectedModifiers");

            sm.WithOwner()
                .HasForeignKey("OrderItemId");

            sm.Property<Guid>("OrderItemId")
                .IsRequired();

            sm.HasKey("OrderItemId", nameof(SelectedModifier.ModifierId));

            sm.Property(m => m.ModifierId)
                .IsRequired();

            sm.Property(m => m.ModifierName)
                .IsRequired()
                .HasMaxLength(200);

            sm.Property(m => m.PriceAdjustment)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
        });

        builder.Navigation(i => i.SelectedModifiers)
            .HasField("_selectedModifiers")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Domain events are transient — never persisted
        builder.Ignore(i => i.DomainEvents);
    }
}
