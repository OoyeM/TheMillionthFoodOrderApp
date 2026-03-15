namespace TheMillionthFoodOrderApp.Domain.Identity;

/// <summary>
/// Repository contract for <see cref="PlatformUser"/> persistence.
/// </summary>
public interface IPlatformUserRepository
{
    /// <summary>Looks up a user by their Entra External ID object ID (the 'sub' / 'oid' claim).</summary>
    Task<PlatformUser?> GetByEntraObjectIdAsync(string entraObjectId, CancellationToken cancellationToken = default);

    /// <summary>Looks up a user by their internal platform ID.</summary>
    Task<PlatformUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Persists a newly created user.</summary>
    Task AddAsync(PlatformUser user, CancellationToken cancellationToken = default);

    /// <summary>Returns all <see cref="BrandUserRole"/> records for the given user.</summary>
    Task<IReadOnlyList<BrandUserRole>> GetRolesForUserAsync(Guid platformUserId, CancellationToken cancellationToken = default);

    /// <summary>Returns all users (with roles) who hold at least one role within the given brand.</summary>
    Task<IReadOnlyList<(PlatformUser User, IReadOnlyList<BrandUserRole> Roles)>> GetUsersByBrandAsync(Guid brandId, CancellationToken cancellationToken = default);

    /// <summary>Persists a new role assignment.</summary>
    Task AddRoleAsync(BrandUserRole role, CancellationToken cancellationToken = default);

    /// <summary>Removes a role assignment.</summary>
    Task RemoveRoleAsync(BrandUserRole role, CancellationToken cancellationToken = default);

    /// <summary>Persists all pending changes to the underlying store.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
