using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.MenuCategories;

namespace TheMillionthFoodOrderApp.Infrastructure.MenuCategories;

public sealed class MenuCategoryConfiguration : IEntityTypeConfiguration<MenuCategory>
{
    public void Configure(EntityTypeBuilder<MenuCategory> builder)
    {
        builder.ToTable("MenuCategories");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ImageUrl)
            .HasMaxLength(2048);

        builder.Property(c => c.SortOrder)
            .IsRequired();

        builder.Property(c => c.IsDeleted)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.HasMany(c => c.Translations)
            .WithOne()
            .HasForeignKey(t => t.MenuCategoryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Domain events are transient — never persisted
        builder.Ignore(c => c.DomainEvents);
    }
}
