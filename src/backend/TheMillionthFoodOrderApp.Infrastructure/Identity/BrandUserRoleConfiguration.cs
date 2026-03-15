using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.Identity;

namespace TheMillionthFoodOrderApp.Infrastructure.Identity;

public sealed class BrandUserRoleConfiguration : IEntityTypeConfiguration<BrandUserRole>
{
    public void Configure(EntityTypeBuilder<BrandUserRole> builder)
    {
        builder.ToTable("BrandUserRoles");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.PlatformUserId)
            .IsRequired();

        builder.Property(r => r.BrandId)
            .IsRequired();

        // ShopId is nullable — null means brand-level role
        builder.Property(r => r.ShopId);

        builder.Property(r => r.Role)
            .IsRequired();

        builder.Property(r => r.CreatedAt)
            .IsRequired();

        // FK to PlatformUser — cascade delete removes all roles when user is deleted
        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(r => r.PlatformUserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevent duplicate role assignments: a user cannot hold the same role at the same
        // brand/shop combination more than once. ShopId is included as nullable (null is valid).
        builder.HasIndex(r => new { r.PlatformUserId, r.BrandId, r.ShopId, r.Role })
            .IsUnique();

        // Domain events are transient — never persisted
        builder.Ignore(r => r.DomainEvents);
    }
}
