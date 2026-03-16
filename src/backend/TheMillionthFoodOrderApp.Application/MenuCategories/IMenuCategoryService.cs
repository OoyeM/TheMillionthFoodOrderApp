using TheMillionthFoodOrderApp.Application.Products;

namespace TheMillionthFoodOrderApp.Application.MenuCategories;

public interface IMenuCategoryService
{
    Task<MenuCategoryResponse> CreateMenuCategoryAsync(CreateMenuCategoryRequest request, CancellationToken cancellationToken = default);
    Task<MenuCategoryResponse> UpdateMenuCategoryAsync(Guid id, UpdateMenuCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteMenuCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<MenuCategoryResponse> GetMenuCategoryAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuCategoryListItemResponse>> GetMenuCategoriesAsync(CancellationToken cancellationToken = default);
    Task ReorderMenuCategoryAsync(Guid id, ReorderMenuCategoryRequest request, CancellationToken cancellationToken = default);
    Task AssignProductCategoryAsync(AssignProductCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all products assigned to <paramref name="categoryId"/>, ordered by their sort position.
    /// </summary>
    Task<IReadOnlyList<ProductListItemResponse>> GetCategoryProductsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reorders products within <paramref name="categoryId"/> according to the ordered list of IDs.
    /// Assigns positions 0..n-1 sequentially. All provided product IDs must belong to the category.
    /// </summary>
    Task ReorderProductsInCategoryAsync(Guid categoryId, ReorderProductsInCategoryRequest request, CancellationToken cancellationToken = default);
}
