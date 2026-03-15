using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.Identity;

namespace TheMillionthFoodOrderApp.Infrastructure.Identity;

public sealed class PlatformUserConfiguration : IEntityTypeConfiguration<PlatformUser>
{
    public void Configure(EntityTypeBuilder<PlatformUser> builder)
    {
        builder.ToTable("PlatformUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.EntraObjectId)
            .IsRequired()
            .HasMaxLength(36); // Azure object IDs are GUIDs (36 chars with hyphens)

        // EntraObjectId must be globally unique — the primary lookup key post-authentication
        builder.HasIndex(u => u.EntraObjectId)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320); // RFC 5321 maximum

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.IsPlatformAdmin)
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .IsRequired();

        // Domain events are transient — never persisted
        builder.Ignore(u => u.DomainEvents);
    }
}
