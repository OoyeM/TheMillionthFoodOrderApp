using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.MenuCategories;

namespace TheMillionthFoodOrderApp.Infrastructure.MenuCategories;

public sealed class MenuCategoryTranslationConfiguration : IEntityTypeConfiguration<MenuCategoryTranslation>
{
    public void Configure(EntityTypeBuilder<MenuCategoryTranslation> builder)
    {
        builder.ToTable("MenuCategoryTranslations");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.MenuCategoryId)
            .IsRequired();

        builder.Property(t => t.LanguageCode)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        // One translation per language per category
        builder.HasIndex(t => new { t.MenuCategoryId, t.LanguageCode })
            .IsUnique();

        // Domain events are transient — never persisted
        builder.Ignore(t => t.DomainEvents);
    }
}
