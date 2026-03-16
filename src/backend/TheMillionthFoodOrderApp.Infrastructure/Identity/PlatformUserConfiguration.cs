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

        builder.Property(u => u.ExternalIdentityId)
            .IsRequired()
            .HasMaxLength(128); // OIDC subject IDs (UUIDs for Keycloak, various formats for other providers)

        // ExternalIdentityId must be globally unique — the primary lookup key post-authentication
        builder.HasIndex(u => u.ExternalIdentityId)
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
