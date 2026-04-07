namespace TheMillionthFoodOrderApp.Application.Products;

public sealed record TranslationRequest(string LanguageCode, string Name, string? Description);
public sealed record TranslationResponse(string LanguageCode, string Name, string? Description);
public sealed record MoneyResponse(decimal Amount, string Currency);

public sealed record CreateProductRequest(
    decimal BasePrice,
    string? ImageUrl,
    IReadOnlyList<TranslationRequest> Translations);

public sealed record UpdateProductRequest(
    decimal BasePrice,
    string? ImageUrl,
    IReadOnlyList<TranslationRequest> Translations);

public sealed record CreateComboProductRequest(
    decimal BasePrice,
    string? ImageUrl,
    IReadOnlyList<TranslationRequest> Translations,
    IReadOnlyList<Guid> ComponentProductIds);

public sealed record UpdateComboProductRequest(
    decimal BasePrice,
    string? ImageUrl,
    IReadOnlyList<TranslationRequest> Translations,
    IReadOnlyList<Guid> ComponentProductIds);

public sealed record ComboItemResponse(Guid ComponentProductId, string Name, int SortOrder);

public sealed record ProductResponse(
    Guid Id,
    string ProductType,
    MoneyResponse BasePrice,
    string? ImageUrl,
    Guid? MenuCategoryId,
    int SortOrderInCategory,
    IReadOnlyList<TranslationResponse> Translations,
    IReadOnlyList<ComboItemResponse>? ComboItems,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProductListItemResponse(
    Guid Id,
    string ProductType,
    string Name,
    MoneyResponse BasePrice,
    string? ImageUrl,
    Guid? MenuCategoryId,
    int SortOrderInCategory,
    DateTimeOffset CreatedAt);
