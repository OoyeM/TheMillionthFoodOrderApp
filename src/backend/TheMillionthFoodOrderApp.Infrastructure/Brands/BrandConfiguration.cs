using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.Brands;

namespace TheMillionthFoodOrderApp.Infrastructure.Brands;

public sealed class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("Brands");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(b => b.Slug)
            .IsRequired()
            .HasMaxLength(100);

        // Slug must be globally unique — enforced at DB level
        builder.HasIndex(b => b.Slug)
            .IsUnique();

        builder.Property(b => b.ContactEmail)
            .IsRequired()
            .HasMaxLength(320); // RFC 5321 maximum

        builder.Property(b => b.ContactPhone)
            .HasMaxLength(30);

        builder.Property(b => b.DatabaseName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(b => b.IsActive)
            .IsRequired();

        builder.Property(b => b.CreatedAt)
            .IsRequired();

        builder.Property(b => b.UpdatedAt)
            .IsRequired();

        // Domain events are transient — never persisted
        builder.Ignore(b => b.DomainEvents);
    }
}
