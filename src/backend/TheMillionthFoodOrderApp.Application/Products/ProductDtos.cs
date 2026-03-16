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

public sealed record ProductResponse(
    Guid Id,
    MoneyResponse BasePrice,
    string? ImageUrl,
    Guid? MenuCategoryId,
    IReadOnlyList<TranslationResponse> Translations,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ProductListItemResponse(
    Guid Id,
    string Name,
    MoneyResponse BasePrice,
    string? ImageUrl,
    Guid? MenuCategoryId,
    DateTimeOffset CreatedAt);
