using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Application.BrandSettings;

public sealed class BrandSettingsService(
    IBrandSettingsRepository repository,
    IFileStorageService fileStorage) : IBrandSettingsService
{
    // ── Default theme values (applied when theming is not yet configured) ────

    private const string DefaultPrimary = "#111827";
    private const string DefaultSecondary = "#6b7280";
    private const string DefaultAccent = "#2563eb";

    public async Task<BrandSettingsResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(cancellationToken);
        return settings is null ? null : MapToResponse(settings);
    }

    public async Task<BrandSettingsResponse> UpsertAsync(
        UpdateBrandSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(cancellationToken);

        if (settings is null)
        {
            settings = Domain.BrandSettings.BrandSettings.Create(
                request.DefaultLanguage,
                request.Timezone,
                request.Currency);

            await repository.AddAsync(settings, cancellationToken);
        }
        else
        {
            settings.Update(request.DefaultLanguage, request.Timezone, request.Currency);
        }

        await repository.SaveChangesAsync(cancellationToken);

        return MapToResponse(settings);
    }

    public async Task<BrandSettingsResponse?> UpdateThemingAsync(
        UpdateBrandThemingRequest request,
        CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(cancellationToken);
        if (settings is null)
            return null;

        BrandColors? colors = null;
        if (request.Colors is not null)
        {
            colors = new BrandColors(
                request.Colors.Primary,
                request.Colors.Secondary,
                request.Colors.Accent);
        }

        BrandTypography? typography = null;
        if (request.Typography is not null)
        {
            typography = new BrandTypography(
                request.Typography.HeadingFontFamily,
                request.Typography.BodyFontFamily);
        }

        settings.UpdateTheming(colors, typography, request.CustomDomain);

        await repository.SaveChangesAsync(cancellationToken);

        return MapToResponse(settings);
    }

    public async Task<UploadBrandLogoResponse?> UploadLogoAsync(
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(cancellationToken);
        if (settings is null)
            return null;

        // Delete the old logo if one exists, to avoid orphaned uploads
        if (!string.IsNullOrWhiteSpace(settings.LogoUrl))
        {
            await fileStorage.DeleteAsync(settings.LogoUrl, cancellationToken);
        }

        var logoUrl = await fileStorage.SaveAsync(fileName, contentType, fileStream, cancellationToken);

        settings.SetLogoUrl(logoUrl);
        await repository.SaveChangesAsync(cancellationToken);

        return new UploadBrandLogoResponse(logoUrl);
    }

    public async Task<BrandThemeResponse?> GetThemeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetAsync(cancellationToken);
        if (settings is null)
            return null;

        return new BrandThemeResponse(
            LogoUrl: settings.LogoUrl,
            CustomDomain: settings.CustomDomain,
            PrimaryColor: settings.Colors?.Primary ?? DefaultPrimary,
            SecondaryColor: settings.Colors?.Secondary ?? DefaultSecondary,
            AccentColor: settings.Colors?.Accent ?? DefaultAccent,
            HeadingFontFamily: settings.Typography?.HeadingFontFamily ?? PresetFonts.SystemDefault,
            BodyFontFamily: settings.Typography?.BodyFontFamily ?? PresetFonts.SystemDefault);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static BrandSettingsResponse MapToResponse(Domain.BrandSettings.BrandSettings settings) =>
        new(
            settings.Id,
            settings.DefaultLanguage,
            settings.Timezone,
            settings.Currency,
            settings.LogoUrl,
            settings.CustomDomain,
            settings.Colors is null ? null : new BrandColorsDto(
                settings.Colors.Primary,
                settings.Colors.Secondary,
                settings.Colors.Accent),
            settings.Typography is null ? null : new BrandTypographyDto(
                settings.Typography.HeadingFontFamily,
                settings.Typography.BodyFontFamily),
            settings.CreatedAt,
            settings.UpdatedAt);
}
