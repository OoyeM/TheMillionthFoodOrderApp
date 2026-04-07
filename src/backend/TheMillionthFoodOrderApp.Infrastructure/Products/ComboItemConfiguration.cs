using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Infrastructure.Products;

public sealed class ComboItemConfiguration : IEntityTypeConfiguration<ComboItem>
{
    public void Configure(EntityTypeBuilder<ComboItem> builder)
    {
        builder.ToTable("ComboItems");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.ComboProductId)
            .IsRequired();

        builder.Property(ci => ci.ComponentProductId)
            .IsRequired();

        builder.Property(ci => ci.SortOrder)
            .IsRequired();

        // Each component product can only appear once per combo
        builder.HasIndex(ci => new { ci.ComboProductId, ci.ComponentProductId })
            .IsUnique();

        // Efficient retrieval of combo items ordered by position
        builder.HasIndex(ci => new { ci.ComboProductId, ci.SortOrder });

        // Cascade-delete combo items when the combo product is deleted
        builder.HasOne<Product>()
            .WithMany(p => p.ComboItems)
            .HasForeignKey(ci => ci.ComboProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict deletion of a product that is a component of a combo
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(ci => ci.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Domain events are transient — never persisted
        builder.Ignore(ci => ci.DomainEvents);
    }
}
