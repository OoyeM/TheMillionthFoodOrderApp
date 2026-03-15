using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Identity;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure.Identity;

public sealed class PlatformUserRepository(PlatformDbContext dbContext) : IPlatformUserRepository
{
    public async Task<PlatformUser?> GetByEntraObjectIdAsync(
        string entraObjectId,
        CancellationToken cancellationToken = default)
        => await dbContext.PlatformUsers
            .FirstOrDefaultAsync(u => u.EntraObjectId == entraObjectId, cancellationToken);

    public async Task<PlatformUser?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await dbContext.PlatformUsers.FindAsync([id], cancellationToken);

    public async Task AddAsync(
        PlatformUser user,
        CancellationToken cancellationToken = default)
        => await dbContext.PlatformUsers.AddAsync(user, cancellationToken);

    public async Task<IReadOnlyList<BrandUserRole>> GetRolesForUserAsync(
        Guid platformUserId,
        CancellationToken cancellationToken = default)
        => await dbContext.BrandUserRoles
            .Where(r => r.PlatformUserId == platformUserId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(PlatformUser User, IReadOnlyList<BrandUserRole> Roles)>> GetUsersByBrandAsync(
        Guid brandId,
        CancellationToken cancellationToken = default)
    {
        // Fetch all roles for the brand, then group by user in memory.
        var brandRoles = await dbContext.BrandUserRoles
            .Where(r => r.BrandId == brandId)
            .ToListAsync(cancellationToken);

        if (brandRoles.Count == 0)
            return [];

        var userIds = brandRoles.Select(r => r.PlatformUserId).Distinct().ToList();

        var users = await dbContext.PlatformUsers
            .Where(u => userIds.Contains(u.Id))
            .ToListAsync(cancellationToken);

        return users
            .Select(u => (u, (IReadOnlyList<BrandUserRole>)brandRoles
                .Where(r => r.PlatformUserId == u.Id)
                .ToList()))
            .ToList();
    }

    public async Task AddRoleAsync(
        BrandUserRole role,
        CancellationToken cancellationToken = default)
        => await dbContext.BrandUserRoles.AddAsync(role, cancellationToken);

    public Task RemoveRoleAsync(
        BrandUserRole role,
        CancellationToken cancellationToken = default)
    {
        dbContext.BrandUserRoles.Remove(role);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);
}
