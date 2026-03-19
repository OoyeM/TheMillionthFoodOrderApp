using TheMillionthFoodOrderApp.Domain.Identity;

namespace TheMillionthFoodOrderApp.Application.Identity;

/// <summary>DTO returned for every brand staff operation.</summary>
public sealed record StaffMemberResponse(
    Guid Id,
    string Email,
    string DisplayName,
    Guid RoleId,
    StaffRole Role,
    Guid? ShopId,
    string? ShopName,
    DateTimeOffset CreatedAt);

/// <summary>Request to invite a staff member to a brand, optionally scoped to a shop.</summary>
public sealed record InviteBrandStaffRequest(
    string Email,
    string DisplayName,
    StaffRole Role,
    Guid? ShopId);

/// <summary>Application service for managing brand-scoped staff accounts.</summary>
public interface IBrandStaffService
{
    /// <summary>Returns all staff members for a brand (one entry per role assignment).</summary>
    Task<IReadOnlyList<StaffMemberResponse>> ListAsync(
        string brandSlug,
        CancellationToken cancellationToken = default);

    /// <summary>Returns all staff members for a specific shop within a brand.</summary>
    Task<IReadOnlyList<StaffMemberResponse>> ListByShopAsync(
        string brandSlug,
        Guid shopId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invites a user as a brand staff member with the specified role.
    /// If the user does not exist, creates a pending user linked to real identity on first OIDC login.
    /// If the user already holds the same role for the brand/shop, throws <see cref="InvalidOperationException"/>.
    /// Shop-level roles require a non-null <see cref="InviteBrandStaffRequest.ShopId"/>.
    /// </summary>
    Task<StaffMemberResponse> InviteAsync(
        string brandSlug,
        InviteBrandStaffRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role assignment from a brand staff member.
    /// Throws <see cref="KeyNotFoundException"/> if the role assignment is not found.
    /// Throws <see cref="InvalidOperationException"/> if removing would leave no BrandAdmin for the brand.
    /// </summary>
    Task DeactivateAsync(
        string brandSlug,
        Guid roleId,
        CancellationToken cancellationToken = default);
}
