using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Identity;

/// <summary>
/// Aggregate root representing a user provisioned from an external identity provider.
/// Each user is uniquely identified by their external identity ID (the 'sub' claim).
/// Platform-level role assignments are stored directly on this entity;
/// brand/shop-scoped assignments live in <see cref="BrandUserRole"/>.
/// </summary>
public sealed class PlatformUser : AggregateRoot<Guid>, IAuditable
{
    /// <summary>External identity provider subject ID (the 'sub' claim). Unique across the platform.</summary>
    public string ExternalIdentityId { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>When true, this user has unrestricted access across all brands.</summary>
    public bool IsPlatformAdmin { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Required by EF Core
    private PlatformUser() { }

    /// <summary>
    /// Factory method — the only way to create a valid PlatformUser.
    /// Called during first-time provisioning after successful authentication.
    /// </summary>
    public static PlatformUser Create(
        string externalIdentityId,
        string email,
        string displayName,
        bool isPlatformAdmin = false)
    {
        var now = DateTimeOffset.UtcNow;

        return new PlatformUser
        {
            Id = Guid.CreateVersion7(),
            ExternalIdentityId = externalIdentityId,
            Email = email,
            DisplayName = displayName,
            IsPlatformAdmin = isPlatformAdmin,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>Synchronises mutable profile data from identity provider token claims.</summary>
    public void UpdateProfile(string email, string displayName)
    {
        Email = email;
        DisplayName = displayName;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Grants platform-admin privileges to this user.</summary>
    public void PromoteToPlatformAdmin()
    {
        IsPlatformAdmin = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Revokes platform-admin privileges from this user.</summary>
    public void RevokePlatformAdmin()
    {
        IsPlatformAdmin = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
