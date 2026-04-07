using TheMillionthFoodOrderApp.Application.Common;
using TheMillionthFoodOrderApp.Domain.BrandSettings;
using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Application.Products;

public sealed class ProductService(
    IProductRepository productRepository,
    IBrandSettingsRepository brandSettingsRepository) : IProductService
{
    private string? _cachedPrimaryLanguage;

    private async Task<string> GetPrimaryLanguageAsync(CancellationToken ct)
    {
        if (_cachedPrimaryLanguage is null)
        {
            var settings = await brandSettingsRepository.GetAsync(ct);
            _cachedPrimaryLanguage = settings?.DefaultLanguage ?? "nl-BE";
        }
        return _cachedPrimaryLanguage;
    }

    public async Task<ProductResponse> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        TranslationResolver.EnsurePrimaryLanguagePresent(
            request.Translations,
            t => t.LanguageCode,
            primaryLanguage,
            "product");

        var money = new Money(request.BasePrice, "EUR");
        var translations = request.Translations
            .Select(t => (t.LanguageCode, t.Name, t.Description));

        var product = Product.Create(money, request.ImageUrl, translations);

        await productRepository.AddAsync(product, cancellationToken);
        await productRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(product);
    }

    public async Task<ProductResponse> UpdateProductAsync(
        Guid id,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        TranslationResolver.EnsurePrimaryLanguagePresent(
            request.Translations,
            t => t.LanguageCode,
            primaryLanguage,
            "product");

        var money = new Money(request.BasePrice, "EUR");
        var translations = request.Translations
            .Select(t => (t.LanguageCode, t.Name, t.Description));

        var product = await productRepository.UpdateAsync(
            id,
            p => p.Update(money, request.ImageUrl, translations),
            cancellationToken);

        if (product is null)
            throw new KeyNotFoundException($"Product with id '{id}' was not found.");

        return MapToResponse(product);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.UpdateAsync(
            id, p => p.SoftDelete(), cancellationToken);

        if (product is null)
            throw new KeyNotFoundException($"Product with id '{id}' was not found.");
    }

    public async Task<ProductResponse> GetProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await productRepository.GetByIdAsync(id, cancellationToken);
        if (product is null)
            throw new KeyNotFoundException($"Product with id '{id}' was not found.");

        return MapToResponse(product);
    }

    public async Task<IReadOnlyList<ProductListItemResponse>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        var products = await productRepository.GetAllAsync(cancellationToken);
        return products.Select(p => MapToListItem(p, primaryLanguage)).ToList().AsReadOnly();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static ProductResponse MapToResponse(Product product) =>
        new(
            product.Id,
            new MoneyResponse(product.BasePrice.Amount, product.BasePrice.Currency),
            product.ImageUrl,
            product.MenuCategoryId,
            product.SortOrderInCategory,
            product.Translations
                .Select(t => new TranslationResponse(t.LanguageCode, t.Name, t.Description))
                .ToList().AsReadOnly(),
            product.CreatedAt,
            product.UpdatedAt);

    private static ProductListItemResponse MapToListItem(Product product, string primaryLanguage) =>
        new(
            product.Id,
            TranslationResolver.ResolveName(
                product.Translations,
                t => t.LanguageCode,
                t => t.Name,
                primaryLanguage),
            new MoneyResponse(product.BasePrice.Amount, product.BasePrice.Currency),
            product.ImageUrl,
            product.MenuCategoryId,
            product.SortOrderInCategory,
            product.CreatedAt);
}
