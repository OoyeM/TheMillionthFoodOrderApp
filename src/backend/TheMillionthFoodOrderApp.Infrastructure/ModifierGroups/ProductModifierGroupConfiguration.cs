using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;

namespace TheMillionthFoodOrderApp.Infrastructure.ModifierGroups;

public sealed class ProductModifierGroupConfiguration : IEntityTypeConfiguration<ProductModifierGroup>
{
    public void Configure(EntityTypeBuilder<ProductModifierGroup> builder)
    {
        builder.ToTable("ProductModifierGroups");

        builder.HasKey(pmg => pmg.Id);

        builder.Property(pmg => pmg.ProductId)
            .IsRequired();

        builder.Property(pmg => pmg.ModifierGroupId)
            .IsRequired();

        builder.Property(pmg => pmg.SortOrder)
            .IsRequired();

        // A product can only have each modifier group assigned once
        builder.HasIndex(pmg => new { pmg.ProductId, pmg.ModifierGroupId })
            .IsUnique();

        // Index to support efficient GetProductModifierGroupsAsync
        builder.HasIndex(pmg => new { pmg.ProductId, pmg.SortOrder })
            .HasDatabaseName("IX_ProductModifierGroups_ProductId_SortOrder");

        // FK to Products (no navigation on Product side — products don't know about modifier groups directly)
        builder.HasOne<Domain.Products.Product>()
            .WithMany()
            .HasForeignKey(pmg => pmg.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // FK to ModifierGroups
        builder.HasOne<ModifierGroup>()
            .WithMany()
            .HasForeignKey(pmg => pmg.ModifierGroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Domain events are transient — never persisted
        builder.Ignore(pmg => pmg.DomainEvents);
    }
}
