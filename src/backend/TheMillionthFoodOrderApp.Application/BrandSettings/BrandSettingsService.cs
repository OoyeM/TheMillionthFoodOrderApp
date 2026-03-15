using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Application.BrandSettings;

public sealed class BrandSettingsService(IBrandSettingsRepository repository) : IBrandSettingsService
{
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

    // ── Private helpers ──────────────────────────────────────────────────────

    private static BrandSettingsResponse MapToResponse(Domain.BrandSettings.BrandSettings settings) =>
        new(
            settings.Id,
            settings.DefaultLanguage,
            settings.Timezone,
            settings.Currency,
            settings.CreatedAt,
            settings.UpdatedAt);
}
