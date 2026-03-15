using TheMillionthFoodOrderApp.Domain.Brands;

namespace TheMillionthFoodOrderApp.Application.Brands;

public interface IBrandService
{
    Task<BrandResponse> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken = default);
    Task<BrandResponse> UpdateBrandAsync(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken = default);
    Task DeactivateBrandAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateBrandAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandResponse> GetBrandAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BrandResponse>> GetBrandsAsync(CancellationToken cancellationToken = default);

    /// <summary>Looks up a brand by slug and returns it, or null if not found.</summary>
    Task<BrandResponse?> GetBrandBySlugAsync(string slug, CancellationToken cancellationToken = default);

    /// <summary>Configures the staff authentication method for the specified brand (looked up by slug).</summary>
    Task<BrandResponse> ConfigureStaffAuthAsync(string slug, StaffAuthMethod method, CancellationToken cancellationToken = default);
}
