using TheMillionthFoodOrderApp.Domain.Brands;

namespace TheMillionthFoodOrderApp.Application.Brands;

public sealed class BrandService(IBrandRepository brandRepository) : IBrandService
{
    public async Task<BrandResponse> CreateBrandAsync(
        CreateBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await brandRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A brand with slug '{request.Slug}' already exists.");

        var brand = Brand.Create(request.Name, request.Slug, request.ContactEmail, request.ContactPhone);

        await brandRepository.AddAsync(brand, cancellationToken);
        await brandRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(brand);
    }

    public async Task<BrandResponse> UpdateBrandAsync(
        Guid id,
        UpdateBrandRequest request,
        CancellationToken cancellationToken = default)
    {
        var brand = await GetOrThrowAsync(id, cancellationToken);

        brand.UpdateMetadata(request.Name, request.ContactEmail, request.ContactPhone);
        await brandRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(brand);
    }

    public async Task DeactivateBrandAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await GetOrThrowAsync(id, cancellationToken);

        brand.Deactivate();
        await brandRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task ActivateBrandAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await GetOrThrowAsync(id, cancellationToken);

        brand.Activate();
        await brandRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<BrandResponse> GetBrandAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await GetOrThrowAsync(id, cancellationToken);
        return MapToResponse(brand);
    }

    public async Task<IReadOnlyList<BrandResponse>> GetBrandsAsync(CancellationToken cancellationToken = default)
    {
        var brands = await brandRepository.GetAllAsync(cancellationToken);
        return brands.Select(MapToResponse).ToList().AsReadOnly();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<Domain.Brands.Brand> GetOrThrowAsync(Guid id, CancellationToken cancellationToken)
    {
        var brand = await brandRepository.GetByIdAsync(id, cancellationToken);
        if (brand is null)
            throw new KeyNotFoundException($"Brand with id '{id}' was not found.");

        return brand;
    }

    private static BrandResponse MapToResponse(Domain.Brands.Brand brand) =>
        new(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.ContactEmail,
            brand.ContactPhone,
            brand.IsActive,
            brand.DatabaseName,
            brand.CreatedAt,
            brand.UpdatedAt);
}
