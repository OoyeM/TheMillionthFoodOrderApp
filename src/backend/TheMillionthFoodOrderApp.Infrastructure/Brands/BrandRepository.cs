using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure.Brands;

public sealed class BrandRepository(PlatformDbContext dbContext) : IBrandRepository
{
    public async Task<Brand?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Brands.FindAsync([id], cancellationToken);

    public async Task<Brand?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await dbContext.Brands
            .FirstOrDefaultAsync(b => b.Slug == slug, cancellationToken);

    public async Task<IReadOnlyList<Brand>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Brands
            .OrderBy(b => b.Name)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Brand brand, CancellationToken cancellationToken = default)
        => await dbContext.Brands.AddAsync(brand, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);
}
