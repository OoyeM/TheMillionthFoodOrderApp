using TheMillionthFoodOrderApp.Domain.MenuCategories;
using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Application.MenuCategories;

public sealed class MenuCategoryService(
    IMenuCategoryRepository menuCategoryRepository,
    IProductRepository productRepository) : IMenuCategoryService
{
    public async Task<MenuCategoryResponse> CreateMenuCategoryAsync(
        CreateMenuCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
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
        var categories = await menuCategoryRepository.GetAllAsync(cancellationToken);
        var productCounts = await menuCategoryRepository.GetProductCountsAsync(cancellationToken);
        return categories
            .Select(c => MapToListItem(c, productCounts.GetValueOrDefault(c.Id, 0)))
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

        var product = await productRepository.UpdateAsync(
            request.ProductId,
            p => p.AssignCategory(request.CategoryId),
            cancellationToken);

        if (product is null)
            throw new KeyNotFoundException($"Product with id '{request.ProductId}' was not found.");
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

    private static MenuCategoryListItemResponse MapToListItem(MenuCategory category, int productCount) =>
        new(
            category.Id,
            category.Translations.FirstOrDefault()?.Name ?? "(unnamed)",
            category.ImageUrl,
            category.SortOrder,
            productCount,
            category.CreatedAt);
}
