using TheMillionthFoodOrderApp.Application.Brands;
using TheMillionthFoodOrderApp.Domain.Identity;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Application.Identity;

public sealed class BrandStaffService(
    IPlatformUserRepository userRepository,
    IBrandService brandService,
    IShopRepository shopRepository) : IBrandStaffService
{
    public async Task<IReadOnlyList<StaffMemberResponse>> ListAsync(
        string brandSlug,
        CancellationToken cancellationToken = default)
    {
        var brand = await ResolveBrandAsync(brandSlug, cancellationToken);
        var usersWithRoles = await userRepository.GetUsersByBrandAsync(brand.Id, cancellationToken);

        if (usersWithRoles.Count == 0)
            return [];

        // Only fetch shops if any roles reference one — avoids unnecessary full-table scan
        var shopIds = usersWithRoles
            .SelectMany(u => u.Roles)
            .Where(r => r.ShopId.HasValue)
            .Select(r => r.ShopId!.Value)
            .Distinct()
            .ToList();

        var shopNameMap = new Dictionary<Guid, string>();
        if (shopIds.Count > 0)
        {
            var shops = await shopRepository.GetAllAsync(cancellationToken);
            shopNameMap = shops.ToDictionary(s => s.Id, s => s.Name);
        }

        return FlattenToResponses(usersWithRoles, shopNameMap);
    }

    public async Task<IReadOnlyList<StaffMemberResponse>> ListByShopAsync(
        string brandSlug,
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var brand = await ResolveBrandAsync(brandSlug, cancellationToken);
        var usersWithRoles = await userRepository.GetUsersByBrandAndShopAsync(brand.Id, shopId, cancellationToken);

        if (usersWithRoles.Count == 0)
            return [];

        var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken);
        var shopNameMap = shop is not null
            ? new Dictionary<Guid, string> { [shopId] = shop.Name }
            : new Dictionary<Guid, string>();

        return FlattenToResponses(usersWithRoles, shopNameMap);
    }

    public async Task<StaffMemberResponse> InviteAsync(
        string brandSlug,
        InviteBrandStaffRequest request,
        CancellationToken cancellationToken = default)
    {
        var brand = await ResolveBrandAsync(brandSlug, cancellationToken);

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);

        if (user is null)
        {
            // Create a pending user — linked to real identity on first OIDC login.
            // Uses AddOrGetExistingAsync to handle concurrent invites for the same email.
            var newUser = PlatformUser.Create(
                externalIdentityId: $"pending:{normalizedEmail}",
                email: normalizedEmail,
                displayName: request.DisplayName,
                isPlatformAdmin: false);

            (user, _) = await userRepository.AddOrGetExistingAsync(newUser, cancellationToken);
        }

        // Guard against duplicate role assignments for the same brand+shop combination
        var existingRoles = await userRepository.GetRolesForUserAsync(user.Id, cancellationToken);
        var duplicate = existingRoles.FirstOrDefault(r =>
            r.BrandId == brand.Id &&
            r.ShopId == request.ShopId &&
            r.Role == request.Role);

        if (duplicate is not null)
            throw new InvalidOperationException(
                $"User '{request.Email}' already holds the role '{request.Role}' for this brand/shop.");

        // BrandUserRole.Create validates shop requirement (throws if shop-level role without ShopId)
        var role = BrandUserRole.Create(
            platformUserId: user.Id,
            brandId: brand.Id,
            shopId: request.ShopId,
            role: request.Role);

        await userRepository.AddRoleAsync(role, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        // Resolve shop name for the response
        string? shopName = null;
        if (request.ShopId.HasValue)
        {
            var shop = await shopRepository.GetByIdAsync(request.ShopId.Value, cancellationToken);
            shopName = shop?.Name;
        }

        return new StaffMemberResponse(
            Id: user.Id,
            Email: user.Email,
            DisplayName: user.DisplayName,
            RoleId: role.Id,
            Role: role.Role,
            ShopId: role.ShopId,
            ShopName: shopName,
            CreatedAt: role.CreatedAt);
    }

    public async Task DeactivateAsync(
        string brandSlug,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        var brand = await ResolveBrandAsync(brandSlug, cancellationToken);

        var role = await userRepository.GetRoleByIdAsync(roleId, cancellationToken);
        if (role is null || role.BrandId != brand.Id)
            throw new KeyNotFoundException($"Role assignment with id '{roleId}' was not found for this brand.");

        // Guard: cannot remove the last BrandAdmin for this brand
        if (role.Role == StaffRole.BrandAdmin)
        {
            var usersWithRoles = await userRepository.GetUsersByBrandAsync(brand.Id, cancellationToken);
            var brandAdminCount = usersWithRoles
                .SelectMany(u => u.Roles)
                .Count(r => r.Role == StaffRole.BrandAdmin);

            if (brandAdminCount <= 1)
                throw new InvalidOperationException(
                    "Cannot remove the last BrandAdmin for this brand.");
        }

        await userRepository.RemoveRoleAsync(role, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<Brands.BrandResponse> ResolveBrandAsync(
        string brandSlug,
        CancellationToken cancellationToken)
    {
        var brand = await brandService.GetBrandBySlugAsync(brandSlug, cancellationToken);
        if (brand is null)
            throw new KeyNotFoundException($"Brand with slug '{brandSlug}' was not found.");
        return brand;
    }

    private static IReadOnlyList<StaffMemberResponse> FlattenToResponses(
        IReadOnlyList<(PlatformUser User, IReadOnlyList<BrandUserRole> Roles)> usersWithRoles,
        Dictionary<Guid, string> shopNameMap)
        => usersWithRoles
            .SelectMany(pair => pair.Roles.Select(role => new StaffMemberResponse(
                Id: pair.User.Id,
                Email: pair.User.Email,
                DisplayName: pair.User.DisplayName,
                RoleId: role.Id,
                Role: role.Role,
                ShopId: role.ShopId,
                ShopName: role.ShopId.HasValue && shopNameMap.TryGetValue(role.ShopId.Value, out var name)
                    ? name
                    : null,
                CreatedAt: role.CreatedAt)))
            .ToList()
            .AsReadOnly();
}
