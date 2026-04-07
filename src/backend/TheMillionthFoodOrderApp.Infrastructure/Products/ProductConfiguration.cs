using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Infrastructure.Products;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.OwnsOne(p => p.BasePrice, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("BasePrice_Amount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("BasePrice_Currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(p => p.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(p => p.IsDeleted)
            .IsRequired();

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt)
            .IsRequired();

        builder.Property(p => p.MenuCategoryId);

        builder.Property(p => p.SortOrderInCategory)
            .IsRequired()
            .HasDefaultValue(0);

        // Composite index used by GetByCategoryAsync to fetch sorted products efficiently.
        builder.HasIndex(p => new { p.MenuCategoryId, p.SortOrderInCategory })
            .HasDatabaseName("IX_Products_MenuCategoryId_SortOrderInCategory");

        // Optional FK to MenuCategories — null means uncategorised.
        // SetNull on delete so products survive category removal.
        builder.HasOne<Domain.MenuCategories.MenuCategory>()
            .WithMany()
            .HasForeignKey(p => p.MenuCategoryId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(p => p.Allergens)
            .HasField("_allergens")
            .HasColumnName("Allergens")
            .HasColumnType("nvarchar(max)");

        builder.Property(p => p.DietaryTags)
            .HasField("_dietaryTags")
            .HasColumnName("DietaryTags")
            .HasColumnType("nvarchar(max)");

        builder.HasMany(p => p.Translations)
            .WithOne()
            .HasForeignKey(t => t.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        // Domain events are transient — never persisted
        builder.Ignore(p => p.DomainEvents);
    }
}
