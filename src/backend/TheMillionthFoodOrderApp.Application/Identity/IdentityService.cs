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

        var newUser = PlatformUser.Create(externalIdentityId, email, displayName);
        var (user, wasCreated) = await userRepository.AddOrGetExistingAsync(newUser, cancellationToken);

        if (!wasCreated)
        {
            // Concurrent insert won — still synchronise the profile
            user.UpdateProfile(email, displayName);
            await userRepository.SaveChangesAsync(cancellationToken);
        }

        return user;
    }

    public async Task<UserWithRolesDto?> GetUserWithRolesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken = default)
    {
        var user = await userRepository.GetByIdAsync(platformUserId, cancellationToken);
        if (user is null)
            return null;

        var rolesWithSlugs = await userRepository.GetRolesWithBrandSlugsAsync(platformUserId, cancellationToken);
        var roleDtos = rolesWithSlugs
            .Select(r => new RoleAssignmentDto(r.Role.BrandId, r.BrandSlug, r.Role.ShopId, r.Role.Role))
            .ToList();

        return new UserWithRolesDto(
            user.Id,
            user.ExternalIdentityId,
            user.Email,
            user.DisplayName,
            user.IsPlatformAdmin,
            user.CreatedAt,
            roleDtos);
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

        var result = new List<UserWithRolesDto>();
        foreach (var (user, roles) in usersWithRoles)
        {
            // For brand staff listing, we already know the brand — fetch slugs for full DTO
            var rolesWithSlugs = await userRepository.GetRolesWithBrandSlugsAsync(user.Id, cancellationToken);
            var roleDtos = rolesWithSlugs
                .Select(r => new RoleAssignmentDto(r.Role.BrandId, r.BrandSlug, r.Role.ShopId, r.Role.Role))
                .ToList();

            result.Add(new UserWithRolesDto(
                user.Id, user.ExternalIdentityId, user.Email, user.DisplayName,
                user.IsPlatformAdmin, user.CreatedAt, roleDtos));
        }

        return result.AsReadOnly();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<PlatformUser> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
            throw new KeyNotFoundException($"PlatformUser with id '{id}' was not found.");

        return user;
    }
}
