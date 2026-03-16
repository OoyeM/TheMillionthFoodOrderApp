using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Identity;

/// <summary>
/// Scoped role assignment for a <see cref="PlatformUser"/> within a brand or shop.
/// - Brand-level roles (e.g. BrandAdmin, Customer): ShopId is null.
/// - Shop-level roles (e.g. ShopManager, CounterStaff, KitchenStaff, FloorStaff): ShopId is required.
/// </summary>
public sealed class BrandUserRole : Entity<Guid>
{
    /// <summary>Foreign key to the owning <see cref="PlatformUser"/>.</summary>
    public Guid PlatformUserId { get; private set; }

    /// <summary>The brand this role is scoped to.</summary>
    public Guid BrandId { get; private set; }

    /// <summary>
    /// The shop this role is scoped to. Null for brand-level roles (BrandAdmin, Customer).
    /// Required for shop-level roles (ShopManager, CounterStaff, KitchenStaff, FloorStaff).
    /// </summary>
    public Guid? ShopId { get; private set; }

    public StaffRole Role { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    // Required by EF Core
    private BrandUserRole() { }

    /// <summary>
    /// Factory method — the only way to create a valid BrandUserRole.
    /// Throws <see cref="InvalidOperationException"/> if a shop-level role is assigned without a ShopId.
    /// </summary>
    public static BrandUserRole Create(
        Guid platformUserId,
        Guid brandId,
        Guid? shopId,
        StaffRole role)
    {
        ValidateShopRequirement(shopId, role);

        return new BrandUserRole
        {
            Id = Guid.CreateVersion7(),
            PlatformUserId = platformUserId,
            BrandId = brandId,
            ShopId = shopId,
            Role = role,
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static readonly HashSet<StaffRole> ShopLevelRoles =
    [
        StaffRole.ShopManager,
        StaffRole.CounterStaff,
        StaffRole.KitchenStaff,
        StaffRole.FloorStaff
    ];

    private static void ValidateShopRequirement(Guid? shopId, StaffRole role)
    {
        if (ShopLevelRoles.Contains(role) && shopId is null)
            throw new InvalidOperationException(
                $"Role '{role}' is a shop-level role and requires a ShopId.");
    }
}
