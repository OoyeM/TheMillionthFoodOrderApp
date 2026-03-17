namespace TheMillionthFoodOrderApp.Application.Identity;

/// <summary>DTO returned for every platform admin operation.</summary>
public sealed record PlatformAdminResponse(
    Guid Id,
    string Email,
    string DisplayName,
    bool IsPlatformAdmin,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Request to invite (create or promote) a platform admin by email.</summary>
public sealed record InvitePlatformAdminRequest(string Email, string DisplayName);

/// <summary>Application service for managing platform-level admin accounts.</summary>
public interface IPlatformAdminService
{
    /// <summary>Returns all users who currently hold platform admin privileges.</summary>
    Task<IReadOnlyList<PlatformAdminResponse>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Invites a user as a platform admin.
    /// If the user already exists and is already an admin, throws <see cref="InvalidOperationException"/>.
    /// If the user exists but is not an admin, promotes them.
    /// If the user does not exist, creates a pending user and grants admin privileges.
    /// </summary>
    Task<PlatformAdminResponse> InviteAsync(InvitePlatformAdminRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes platform admin privileges from the specified user.
    /// Throws <see cref="KeyNotFoundException"/> if the user is not found.
    /// Throws <see cref="InvalidOperationException"/> if this is the last platform admin.
    /// </summary>
    Task DeactivateAsync(Guid platformUserId, CancellationToken cancellationToken = default);
}
