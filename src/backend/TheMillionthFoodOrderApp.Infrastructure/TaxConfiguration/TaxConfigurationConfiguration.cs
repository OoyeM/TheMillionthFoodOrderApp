using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaxConfigDomain = TheMillionthFoodOrderApp.Domain.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Infrastructure.TaxConfiguration;

public sealed class TaxConfigurationConfiguration : IEntityTypeConfiguration<TaxConfigDomain.TaxConfiguration>
{
    public void Configure(EntityTypeBuilder<TaxConfigDomain.TaxConfiguration> builder)
    {
        builder.ToTable("TaxConfigurations");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt)
            .IsRequired();

        builder.Navigation(c => c.VatRates)
            .HasField("_vatRates");

        builder.HasMany(c => c.VatRates)
            .WithOne()
            .HasForeignKey(v => v.TaxConfigurationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Domain events are transient — never persisted
        builder.Ignore(c => c.DomainEvents);
    }
}
