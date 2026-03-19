namespace TheMillionthFoodOrderApp.Application.BrandSettings;

// ── Shared sub-DTOs ──────────────────────────────────────────────────────────

/// <summary>
/// Color palette for a brand. All values are CSS hex color strings (e.g. "#2563eb").
/// </summary>
public sealed record BrandColorsDto(
    string Primary,
    string Secondary,
    string Accent);

/// <summary>
/// Typography settings for a brand. Font families must be selected from the preset list.
/// </summary>
public sealed record BrandTypographyDto(
    string HeadingFontFamily,
    string BodyFontFamily);

// ── Settings DTOs ────────────────────────────────────────────────────────────

/// <summary>
/// Response DTO for brand settings — returned by GET and PUT endpoints.
/// </summary>
public sealed record BrandSettingsResponse(
    Guid Id,
    string DefaultLanguage,
    string Timezone,
    string Currency,
    string? LogoUrl,
    string? CustomDomain,
    BrandColorsDto? Colors,
    BrandTypographyDto? Typography,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Request DTO for updating brand settings (general locale/timezone/currency fields).
/// </summary>
public sealed record UpdateBrandSettingsRequest(
    string DefaultLanguage,
    string Timezone,
    string Currency);

/// <summary>
/// Request DTO for updating the brand's visual theming configuration.
/// All theming fields are optional — pass null to clear the respective value.
/// </summary>
public sealed record UpdateBrandThemingRequest(
    BrandColorsDto? Colors,
    BrandTypographyDto? Typography,
    string? CustomDomain);

// ── Theme endpoint DTO ───────────────────────────────────────────────────────

/// <summary>
/// Lightweight public response returned by <c>GET /api/brands/{brandSlug}/theme</c>.
/// Contains only the visual properties required by the storefront to apply CSS theming.
/// This endpoint is anonymous — no auth required.
/// </summary>
public sealed record BrandThemeResponse(
    string? LogoUrl,
    string? CustomDomain,
    string PrimaryColor,
    string SecondaryColor,
    string AccentColor,
    string HeadingFontFamily,
    string BodyFontFamily);

// ── Logo upload ──────────────────────────────────────────────────────────────

/// <summary>
/// Response returned after a successful logo upload.
/// </summary>
public sealed record UploadBrandLogoResponse(string LogoUrl);
