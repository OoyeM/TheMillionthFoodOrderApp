namespace TheMillionthFoodOrderApp.Application.Brands;

public sealed record CreateBrandRequest(
    string Name,
    string Slug,
    string ContactEmail,
    string? ContactPhone);

public sealed record UpdateBrandRequest(
    string Name,
    string ContactEmail,
    string? ContactPhone);

public sealed record BrandResponse(
    Guid Id,
    string Name,
    string Slug,
    string ContactEmail,
    string? ContactPhone,
    bool IsActive,
    string DatabaseName,
    DateTime CreatedAt,
    DateTime UpdatedAt);
