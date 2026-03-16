namespace TheMillionthFoodOrderApp.Application.MenuCategories;

public sealed record CreateMenuCategoryRequest(
    string? ImageUrl,
    int SortOrder,
    IReadOnlyList<MenuCategoryTranslationRequest> Translations);

public sealed record UpdateMenuCategoryRequest(
    string? ImageUrl,
    int SortOrder,
    IReadOnlyList<MenuCategoryTranslationRequest> Translations);

public sealed record ReorderMenuCategoryRequest(int SortOrder);

public sealed record AssignProductCategoryRequest(Guid ProductId, Guid CategoryId);

public sealed record ReorderProductsInCategoryRequest(IReadOnlyList<Guid> ProductIds);

public sealed record MenuCategoryTranslationRequest(string LanguageCode, string Name, string? Description);

public sealed record MenuCategoryTranslationResponse(string LanguageCode, string Name, string? Description);

public sealed record MenuCategoryResponse(
    Guid Id,
    string? ImageUrl,
    int SortOrder,
    IReadOnlyList<MenuCategoryTranslationResponse> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record MenuCategoryListItemResponse(
    Guid Id,
    string Name,
    string? ImageUrl,
    int SortOrder,
    int ProductCount,
    DateTimeOffset CreatedAt);
