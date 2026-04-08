using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Infrastructure.TaxConfiguration;

public sealed class VatRateConfiguration : IEntityTypeConfiguration<VatRate>
{
    public void Configure(EntityTypeBuilder<VatRate> builder)
    {
        builder.ToTable("VatRates");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.ConsumptionMode)
            .IsRequired()
            .HasConversion<string>();

        builder.Property(v => v.RatePercentage)
            .IsRequired()
            .HasPrecision(5, 2);

        builder.Property(v => v.TaxConfigurationId)
            .IsRequired();

        // Enforce one VAT rate per consumption mode per tax configuration
        builder.HasIndex(v => new { v.TaxConfigurationId, v.ConsumptionMode })
            .IsUnique();

        // Domain events are transient — never persisted
        builder.Ignore(v => v.DomainEvents);
    }
}
