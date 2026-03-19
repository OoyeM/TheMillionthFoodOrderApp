using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;

namespace TheMillionthFoodOrderApp.Infrastructure.ModifierGroups;

public sealed class ModifierGroupTranslationConfiguration : IEntityTypeConfiguration<ModifierGroupTranslation>
{
    public void Configure(EntityTypeBuilder<ModifierGroupTranslation> builder)
    {
        builder.ToTable("ModifierGroupTranslations");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ModifierGroupId)
            .IsRequired();

        builder.Property(t => t.LanguageCode)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        // One translation per language per modifier group
        builder.HasIndex(t => new { t.ModifierGroupId, t.LanguageCode })
            .IsUnique();

        // Domain events are transient — never persisted
        builder.Ignore(t => t.DomainEvents);
    }
}
