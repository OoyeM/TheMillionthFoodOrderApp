using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Infrastructure.Orders;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.ShopId)
            .IsRequired();

        builder.Property(o => o.BrandSlug)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.OrderType)
            .IsRequired();

        builder.Property(o => o.PaymentMethod)
            .IsRequired()
            .HasColumnType("int")
            .HasDefaultValue(Domain.Orders.PaymentMethod.CashAtPickup);

        builder.Property(o => o.StatusName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.CustomerName)
            .HasMaxLength(200);

        builder.Property(o => o.VatRatePercent)
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(o => o.SubtotalGross)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.TotalVatAmount)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.TotalNet)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.TotalGross)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.TableNumber)
            .IsRequired(false);

        builder.Property(o => o.CreatedByStaffId)
            .HasColumnType("uniqueidentifier")
            .IsRequired(false);

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.UpdatedAt)
            .IsRequired();

        // Unique order number per shop
        builder.HasIndex(o => new { o.ShopId, o.OrderNumber })
            .IsUnique()
            .HasDatabaseName("UX_Orders_ShopId_OrderNumber");

        // Index for querying orders by shop
        builder.HasIndex(o => o.ShopId)
            .HasDatabaseName("IX_Orders_ShopId");

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(o => o.Items)
            .HasField("_items")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Domain events are transient — never persisted
        builder.Ignore(o => o.DomainEvents);
    }
}
