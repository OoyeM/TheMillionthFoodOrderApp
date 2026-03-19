using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;

namespace TheMillionthFoodOrderApp.Infrastructure.ModifierGroups;

public sealed class ModifierTranslationConfiguration : IEntityTypeConfiguration<ModifierTranslation>
{
    public void Configure(EntityTypeBuilder<ModifierTranslation> builder)
    {
        builder.ToTable("ModifierTranslations");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.ModifierId)
            .IsRequired();

        builder.Property(t => t.LanguageCode)
            .IsRequired()
            .HasMaxLength(5);

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        // One translation per language per modifier
        builder.HasIndex(t => new { t.ModifierId, t.LanguageCode })
            .IsUnique();

        // Domain events are transient — never persisted
        builder.Ignore(t => t.DomainEvents);
    }
}
