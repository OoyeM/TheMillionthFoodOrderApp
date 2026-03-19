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

        // ── Theming ──────────────────────────────────────────────────────────

        builder.Property(s => s.LogoUrl)
            .HasMaxLength(2048) // Standard URL max length
            .IsRequired(false);

        builder.Property(s => s.CustomDomain)
            .HasMaxLength(253) // Max DNS label length per RFC 1035
            .IsRequired(false);

        // BrandColors owned entity — nullable, stored as flat columns on the same table.
        // All three color columns are nullable because the entire Colors object is optional.
        builder.OwnsOne(s => s.Colors, colors =>
        {
            colors.Property(c => c.Primary)
                .HasColumnName("Colors_Primary")
                .HasMaxLength(7) // "#rrggbb"
                .IsRequired(false);

            colors.Property(c => c.Secondary)
                .HasColumnName("Colors_Secondary")
                .HasMaxLength(7)
                .IsRequired(false);

            colors.Property(c => c.Accent)
                .HasColumnName("Colors_Accent")
                .HasMaxLength(7)
                .IsRequired(false);
        });

        // BrandTypography owned entity — nullable, stored as flat columns on the same table.
        builder.OwnsOne(s => s.Typography, typography =>
        {
            typography.Property(t => t.HeadingFontFamily)
                .HasColumnName("Typography_HeadingFontFamily")
                .HasMaxLength(100)
                .IsRequired(false);

            typography.Property(t => t.BodyFontFamily)
                .HasColumnName("Typography_BodyFontFamily")
                .HasMaxLength(100)
                .IsRequired(false);
        });

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Domain events are transient — never persisted
        builder.Ignore(s => s.DomainEvents);
    }
}
