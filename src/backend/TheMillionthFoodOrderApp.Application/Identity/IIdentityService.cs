using TheMillionthFoodOrderApp.Domain.Identity;

namespace TheMillionthFoodOrderApp.Application.Identity;

/// <summary>
/// Application-layer contract for user identity and role management.
/// </summary>
public interface IIdentityService
{
    /// <summary>
    /// Provisions a user on first login (idempotent — safe to call on every login).
    /// If the user already exists, their email and display name are synchronised from the latest token.
    /// </summary>
    Task<PlatformUser> ProvisionUserAsync(
        string externalIdentityId,
        string email,
        string displayName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a user together with all their brand/shop role assignments.
    /// Returns null when the user has not yet been provisioned.
    /// </summary>
    Task<UserWithRolesDto?> GetUserWithRolesAsync(
        Guid platformUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user within a brand (and optionally a shop).
    /// Throws <see cref="InvalidOperationException"/> when the assignment already exists.
    /// </summary>
    Task AssignRoleAsync(
        Guid platformUserId,
        Guid brandId,
        Guid? shopId,
        StaffRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific role assignment from a user.
    /// Throws <see cref="KeyNotFoundException"/> when the assignment does not exist.
    /// </summary>
    Task RemoveRoleAsync(
        Guid platformUserId,
        Guid brandId,
        Guid? shopId,
        StaffRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all staff members (users with at least one role) for a given brand.
    /// </summary>
    Task<IReadOnlyList<UserWithRolesDto>> GetBrandStaffAsync(
        Guid brandId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Read model that combines a <see cref="PlatformUser"/> with their role assignments.
/// </summary>
public sealed record UserWithRolesDto(
    Guid Id,
    string ExternalIdentityId,
    string Email,
    string DisplayName,
    bool IsPlatformAdmin,
    DateTimeOffset CreatedAt,
    IReadOnlyList<RoleAssignmentDto> Roles);

/// <summary>
/// Role assignment with the brand slug resolved (for claims enrichment).
/// </summary>
public sealed record RoleAssignmentDto(
    Guid BrandId,
    string BrandSlug,
    Guid? ShopId,
    StaffRole Role);
