using TheMillionthFoodOrderApp.Application.Common;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Domain.BrandSettings;
using TheMillionthFoodOrderApp.Domain.MenuCategories;
using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Application.MenuCategories;

public sealed class MenuCategoryService(
    IMenuCategoryRepository menuCategoryRepository,
    IProductRepository productRepository,
    IBrandSettingsRepository brandSettingsRepository) : IMenuCategoryService
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

    public async Task<MenuCategoryResponse> CreateMenuCategoryAsync(
        CreateMenuCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        TranslationResolver.EnsurePrimaryLanguagePresent(
            request.Translations,
            t => t.LanguageCode,
            primaryLanguage,
            "menu category");

        var translations = request.Translations
            .Select(t => (t.LanguageCode, t.Name, t.Description));

        var category = MenuCategory.Create(request.ImageUrl, request.SortOrder, translations);

        await menuCategoryRepository.AddAsync(category, cancellationToken);
        await menuCategoryRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(category);
    }

    public async Task<MenuCategoryResponse> UpdateMenuCategoryAsync(
        Guid id,
        UpdateMenuCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        TranslationResolver.EnsurePrimaryLanguagePresent(
            request.Translations,
            t => t.LanguageCode,
            primaryLanguage,
            "menu category");

        var translations = request.Translations
            .Select(t => (t.LanguageCode, t.Name, t.Description));

        var category = await menuCategoryRepository.UpdateAsync(
            id,
            c => c.Update(request.ImageUrl, request.SortOrder, translations),
            cancellationToken);

        if (category is null)
            throw new KeyNotFoundException($"Menu category with id '{id}' was not found.");

        return MapToResponse(category);
    }

    public async Task DeleteMenuCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await menuCategoryRepository.UpdateScalarAsync(
            id, c => c.SoftDelete(), cancellationToken);

        if (category is null)
            throw new KeyNotFoundException($"Menu category with id '{id}' was not found.");
    }

    public async Task<MenuCategoryResponse> GetMenuCategoryAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var category = await menuCategoryRepository.GetByIdAsync(id, cancellationToken);
        if (category is null)
            throw new KeyNotFoundException($"Menu category with id '{id}' was not found.");

        return MapToResponse(category);
    }

    public async Task<IReadOnlyList<MenuCategoryListItemResponse>> GetMenuCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        var categories = await menuCategoryRepository.GetAllAsync(cancellationToken);
        var productCounts = await menuCategoryRepository.GetProductCountsAsync(cancellationToken);
        return categories
            .Select(c => MapToListItem(c, productCounts.GetValueOrDefault(c.Id, 0), primaryLanguage))
            .ToList()
            .AsReadOnly();
    }

    public async Task ReorderMenuCategoryAsync(
        Guid id,
        ReorderMenuCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        var category = await menuCategoryRepository.UpdateScalarAsync(
            id, c => c.Reorder(request.SortOrder), cancellationToken);

        if (category is null)
            throw new KeyNotFoundException($"Menu category with id '{id}' was not found.");
    }

    public async Task AssignProductCategoryAsync(
        AssignProductCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        // Verify category exists
        var category = await menuCategoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);
        if (category is null)
            throw new KeyNotFoundException($"Menu category with id '{request.CategoryId}' was not found.");

        // Place new product at the end of the category.
        // Note: not atomic — concurrent assigns can produce duplicate sort positions (acceptable for MVP, last-write-wins).
        var maxSortOrder = await productRepository.GetMaxSortOrderInCategoryAsync(request.CategoryId, cancellationToken);

        var product = await productRepository.UpdateAsync(
            request.ProductId,
            p => p.AssignCategory(request.CategoryId, maxSortOrder + 1),
            cancellationToken);

        if (product is null)
            throw new KeyNotFoundException($"Product with id '{request.ProductId}' was not found.");
    }

    public async Task<IReadOnlyList<ProductListItemResponse>> GetCategoryProductsAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        // Verify category exists before fetching its products
        var category = await menuCategoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            throw new KeyNotFoundException($"Menu category with id '{categoryId}' was not found.");

        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        var products = await productRepository.GetByCategoryAsync(categoryId, cancellationToken);
        return products.Select(p => MapProductToListItem(p, primaryLanguage)).ToList().AsReadOnly();
    }

    public async Task ReorderProductsInCategoryAsync(
        Guid categoryId,
        ReorderProductsInCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        // Verify category exists
        var category = await menuCategoryRepository.GetByIdAsync(categoryId, cancellationToken);
        if (category is null)
            throw new KeyNotFoundException($"Menu category with id '{categoryId}' was not found.");

        // Load the products by the provided IDs
        var products = await productRepository.GetByIdsAsync(request.ProductIds, cancellationToken);

        // Validate all submitted IDs were found in the database
        var notFound = request.ProductIds
            .Except(products.Select(p => p.Id))
            .ToList();

        if (notFound.Count > 0)
            throw new KeyNotFoundException(
                $"The following product IDs were not found: {string.Join(", ", notFound)}");

        // Validate all provided product IDs belong to this category
        var wrongCategory = products
            .Where(p => p.MenuCategoryId != categoryId)
            .Select(p => p.Id)
            .ToList();

        if (wrongCategory.Count > 0)
            throw new InvalidOperationException(
                $"The following products do not belong to category '{categoryId}': {string.Join(", ", wrongCategory)}");

        // Index the loaded products by ID for ordered assignment
        var productMap = products.ToDictionary(p => p.Id);

        for (var i = 0; i < request.ProductIds.Count; i++)
        {
            if (productMap.TryGetValue(request.ProductIds[i], out var product))
                product.ReorderInCategory(i);
        }

        await productRepository.SaveChangesAsync(cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static MenuCategoryResponse MapToResponse(MenuCategory category) =>
        new(
            category.Id,
            category.ImageUrl,
            category.SortOrder,
            category.Translations
                .Select(t => new MenuCategoryTranslationResponse(t.LanguageCode, t.Name, t.Description))
                .ToList().AsReadOnly(),
            category.CreatedAt,
            category.UpdatedAt);

    private static MenuCategoryListItemResponse MapToListItem(
        MenuCategory category, int productCount, string primaryLanguage) =>
        new(
            category.Id,
            TranslationResolver.ResolveName(
                category.Translations,
                t => t.LanguageCode,
                t => t.Name,
                primaryLanguage),
            category.ImageUrl,
            category.SortOrder,
            productCount,
            category.CreatedAt);

    private static ProductListItemResponse MapProductToListItem(Product product, string primaryLanguage) =>
        new(
            product.Id,
            product.ProductType.ToString(),
            TranslationResolver.ResolveName(
                product.Translations,
                t => t.LanguageCode,
                t => t.Name,
                primaryLanguage),
            new MoneyResponse(product.BasePrice.Amount, product.BasePrice.Currency),
            product.ImageUrl,
            product.MenuCategoryId,
            product.SortOrderInCategory,
            product.Allergens.Select(a => (int)a).ToList().AsReadOnly(),
            product.DietaryTags.Select(d => (int)d).ToList().AsReadOnly(),
            product.CreatedAt);
}
