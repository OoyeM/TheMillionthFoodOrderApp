using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Infrastructure.BrandSettings;

public sealed class BrandSettingsConfiguration : IEntityTypeConfiguration<Domain.BrandSettings.BrandSettings>
{
    public void Configure(EntityTypeBuilder<Domain.BrandSettings.BrandSettings> builder)
    {
        builder.ToTable("BrandSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.DefaultLanguage)
            .IsRequired()
            .HasMaxLength(20); // BCP-47 language tags are short (e.g. "nl-BE")

        builder.Property(s => s.Timezone)
            .IsRequired()
            .HasMaxLength(100); // IANA timezone identifiers (e.g. "America/New_York")

        builder.Property(s => s.Currency)
            .IsRequired()
            .HasMaxLength(3); // ISO 4217 codes are exactly 3 characters

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Domain events are transient — never persisted
        builder.Ignore(s => s.DomainEvents);
    }
}
