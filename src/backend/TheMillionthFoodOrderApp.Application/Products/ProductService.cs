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

        return MapToResponse(product, componentNames: null);
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

        return MapToResponse(product, componentNames: null);
    }

    public async Task<ProductResponse> CreateComboProductAsync(
        CreateComboProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var componentProducts = await ValidateComponentProductsAsync(request.ComponentProductIds, cancellationToken);

        var money = new Money(request.BasePrice, "EUR");
        var translations = request.Translations
            .Select(t => (t.LanguageCode, t.Name, t.Description));

        var product = Product.CreateCombo(money, request.ImageUrl, translations, request.ComponentProductIds);

        await productRepository.AddAsync(product, cancellationToken);
        await productRepository.SaveChangesAsync(cancellationToken);

        var componentNames = BuildComponentNameLookup(componentProducts);
        return MapToResponse(product, componentNames);
    }

    public async Task<ProductResponse> UpdateComboProductAsync(
        Guid id,
        UpdateComboProductRequest request,
        CancellationToken cancellationToken = default)
    {
        var componentProducts = await ValidateComponentProductsAsync(request.ComponentProductIds, cancellationToken);

        var money = new Money(request.BasePrice, "EUR");
        var translations = request.Translations
            .Select(t => (t.LanguageCode, t.Name, t.Description));

        var product = await productRepository.UpdateAsync(
            id,
            p =>
            {
                p.Update(money, request.ImageUrl, translations);
                p.UpdateComboItems(request.ComponentProductIds);
            },
            cancellationToken);

        if (product is null)
            throw new KeyNotFoundException($"Combo product with id '{id}' was not found.");

        var componentNames = BuildComponentNameLookup(componentProducts);
        return MapToResponse(product, componentNames);
    }

    public async Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var isComponent = await productRepository.IsComponentOfAnyComboAsync(id, cancellationToken);
        if (isComponent)
            throw new InvalidOperationException(
                $"Product with id '{id}' cannot be deleted because it is a component of one or more combo products. Remove it from all combos first.");

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

        Dictionary<Guid, string>? componentNames = null;
        if (product.ProductType == ProductType.Combo && product.ComboItems.Count > 0)
        {
            var componentIds = product.ComboItems.Select(ci => ci.ComponentProductId);
            var components = await productRepository.GetByIdsAsync(componentIds, cancellationToken);
            componentNames = BuildComponentNameLookup(components);
        }

        return MapToResponse(product, componentNames);
    }

    public async Task<IReadOnlyList<ProductListItemResponse>> GetProductsAsync(
        CancellationToken cancellationToken = default)
    {
        var products = await productRepository.GetAllAsync(cancellationToken);
        return products.Select(MapToListItem).ToList().AsReadOnly();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private async Task<IReadOnlyList<Product>> ValidateComponentProductsAsync(
        IReadOnlyList<Guid> componentProductIds,
        CancellationToken cancellationToken)
    {
        var componentProducts = await productRepository.GetByIdsAsync(componentProductIds, cancellationToken);

        if (componentProducts.Count != componentProductIds.Count)
        {
            var missing = componentProductIds.Except(componentProducts.Select(p => p.Id));
            throw new KeyNotFoundException(
                $"Component product(s) not found: {string.Join(", ", missing)}");
        }

        var nonSimple = componentProducts.Where(p => p.ProductType != ProductType.Simple).ToList();
        if (nonSimple.Count > 0)
        {
            throw new InvalidOperationException(
                $"Only simple products can be combo components. The following are not simple: {string.Join(", ", nonSimple.Select(p => p.Id))}");
        }

        return componentProducts;
    }

    private static Dictionary<Guid, string> BuildComponentNameLookup(IReadOnlyList<Product> components) =>
        components.ToDictionary(
            p => p.Id,
            p => p.Translations.FirstOrDefault()?.Name ?? "(unnamed)");

    private static ProductResponse MapToResponse(Product product, Dictionary<Guid, string>? componentNames) =>
        new(
            product.Id,
            product.ProductType.ToString(),
            new MoneyResponse(product.BasePrice.Amount, product.BasePrice.Currency),
            product.ImageUrl,
            product.MenuCategoryId,
            product.SortOrderInCategory,
            product.Translations
                .Select(t => new TranslationResponse(t.LanguageCode, t.Name, t.Description))
                .ToList().AsReadOnly(),
            product.ProductType == ProductType.Combo
                ? product.ComboItems
                    .OrderBy(ci => ci.SortOrder)
                    .Select(ci => new ComboItemResponse(
                        ci.ComponentProductId,
                        componentNames?.GetValueOrDefault(ci.ComponentProductId, "(unnamed)") ?? "(unnamed)",
                        ci.SortOrder))
                    .ToList().AsReadOnly()
                : null,
            product.CreatedAt,
            product.UpdatedAt);

    private static ProductListItemResponse MapToListItem(Product product) =>
        new(
            product.Id,
            product.ProductType.ToString(),
            product.Translations.FirstOrDefault()?.Name ?? "(unnamed)",
            new MoneyResponse(product.BasePrice.Amount, product.BasePrice.Currency),
            product.ImageUrl,
            product.MenuCategoryId,
            product.SortOrderInCategory,
            product.CreatedAt);
}
