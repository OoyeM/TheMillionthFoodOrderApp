using TheMillionthFoodOrderApp.Domain.Identity;

namespace TheMillionthFoodOrderApp.Application.Identity;

public sealed class IdentityService(IPlatformUserRepository userRepository) : IIdentityService
{
    public async Task<PlatformUser> ProvisionUserAsync(
        string externalIdentityId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        var existing = await userRepository.GetByExternalIdentityIdAsync(externalIdentityId, cancellationToken);

        if (existing is not null)
        {
            // Synchronise mutable claims that may have changed in the identity provider
            existing.UpdateProfile(email, displayName);
            await userRepository.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var user = PlatformUser.Create(externalIdentityId, email, displayName);
        await userRepository.AddAsync(user, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return user;
    }

    public async Task<UserWithRolesDto?> GetUserWithRolesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(platformUserId, cancellationToken);
        if (user is null)
            return null;

        var roles = await userRepository.GetRolesForUserAsync(platformUserId, cancellationToken);

        return MapToDto(user, roles);
    }

    public async Task AssignRoleAsync(
        Guid platformUserId,
        Guid brandId,
        Guid? shopId,
        StaffRole role,
        CancellationToken cancellationToken = default)
    {
        var user = await GetOrThrowAsync(platformUserId, cancellationToken);

        var existingRoles = await userRepository.GetRolesForUserAsync(user.Id, cancellationToken);
        var duplicate = existingRoles.FirstOrDefault(r =>
            r.BrandId == brandId &&
            r.ShopId == shopId &&
            r.Role == role);

        if (duplicate is not null)
            throw new InvalidOperationException(
                $"User '{platformUserId}' already holds role '{role}' for brand '{brandId}'" +
                (shopId.HasValue ? $" / shop '{shopId}'" : string.Empty) + ".");

        var assignment = BrandUserRole.Create(platformUserId, brandId, shopId, role);
        await userRepository.AddRoleAsync(assignment, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task RemoveRoleAsync(
        Guid platformUserId,
        Guid brandId,
        Guid? shopId,
        StaffRole role,
        CancellationToken cancellationToken = default)
    {
        var roles = await userRepository.GetRolesForUserAsync(platformUserId, cancellationToken);
        var assignment = roles.FirstOrDefault(r =>
            r.BrandId == brandId &&
            r.ShopId == shopId &&
            r.Role == role);

        if (assignment is null)
            throw new KeyNotFoundException(
                $"No '{role}' assignment found for user '{platformUserId}' at brand '{brandId}'" +
                (shopId.HasValue ? $" / shop '{shopId}'" : string.Empty) + ".");

        await userRepository.RemoveRoleAsync(assignment, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<UserWithRolesDto>> GetBrandStaffAsync(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        var usersWithRoles = await userRepository.GetUsersByBrandAsync(brandId, cancellationToken);

        return usersWithRoles
            .Select(entry => MapToDto(entry.User, entry.Roles))
            .ToList()
            .AsReadOnly();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<PlatformUser> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
            throw new KeyNotFoundException($"PlatformUser with id '{id}' was not found.");

        return user;
    }

    private static UserWithRolesDto MapToDto(PlatformUser user, IReadOnlyList<BrandUserRole> roles) =>
        new(
            user.Id,
            user.ExternalIdentityId,
            user.Email,
            user.DisplayName,
            user.IsPlatformAdmin,
            user.CreatedAt,
            roles);
}
