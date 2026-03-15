namespace TheMillionthFoodOrderApp.Application.BrandSettings;

/// <summary>
/// Response DTO for brand settings — returned by GET and PUT endpoints.
/// </summary>
public sealed record BrandSettingsResponse(
    Guid Id,
    string DefaultLanguage,
    string Timezone,
    string Currency,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Request DTO for updating brand settings.
/// </summary>
public sealed record UpdateBrandSettingsRequest(
    string DefaultLanguage,
    string Timezone,
    string Currency);
