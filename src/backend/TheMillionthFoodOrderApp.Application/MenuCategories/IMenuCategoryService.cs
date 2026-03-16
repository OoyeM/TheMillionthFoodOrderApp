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
}
