using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Application.Products;

public sealed class ProductService(IProductRepository productRepository) : IProductService
{
    public async Task<ProductResponse> CreateProductAsync(
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
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
        var products = await productRepository.GetAllAsync(cancellationToken);
        return products.Select(MapToListItem).ToList().AsReadOnly();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static ProductResponse MapToResponse(Product product) =>
        new(
            product.Id,
            new MoneyResponse(product.BasePrice.Amount, product.BasePrice.Currency),
            product.ImageUrl,
            product.Translations
                .Select(t => new TranslationResponse(t.LanguageCode, t.Name, t.Description))
                .ToList().AsReadOnly(),
            product.CreatedAt,
            product.UpdatedAt);

    private static ProductListItemResponse MapToListItem(Product product) =>
        new(
            product.Id,
            product.Translations.FirstOrDefault()?.Name ?? "(unnamed)",
            new MoneyResponse(product.BasePrice.Amount, product.BasePrice.Currency),
            product.ImageUrl,
            product.CreatedAt);
}
