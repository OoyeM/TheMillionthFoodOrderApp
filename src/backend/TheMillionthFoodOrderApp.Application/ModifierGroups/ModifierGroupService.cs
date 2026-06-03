using TheMillionthFoodOrderApp.Application.Common;
using TheMillionthFoodOrderApp.Domain.BrandSettings;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;

namespace TheMillionthFoodOrderApp.Application.ModifierGroups;

public sealed class ModifierGroupService(
    IModifierGroupRepository modifierGroupRepository,
    IBrandSettingsRepository brandSettingsRepository) : IModifierGroupService
{
    private string? _cachedPrimaryLanguage;

    private async Task<string> GetPrimaryLanguageAsync(CancellationToken ct)
    {
        if (_cachedPrimaryLanguage is null)
        {
            var settings = await brandSettingsRepository.GetAsync(ct);
            _cachedPrimaryLanguage = settings?.DefaultLanguage ?? "nl-BE";
        }
        return _cachedPrimaryLanguage;
    }

    public async Task<ModifierGroupResponse> CreateModifierGroupAsync(
        CreateModifierGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        TranslationResolver.EnsurePrimaryLanguagePresent(
            request.Translations,
            t => t.LanguageCode,
            primaryLanguage,
            "modifier group");

        var groupTranslations = request.Translations
            .Select(t => (t.LanguageCode, t.Name));

        var modifiers = request.Modifiers
            .Select(m => (
                m.PriceAdjustment,
                m.SortOrder,
                (IEnumerable<(string languageCode, string name)>)m.Translations
                    .Select(t => (t.LanguageCode, t.Name))));

        var group = ModifierGroup.Create(groupTranslations, modifiers);

        await modifierGroupRepository.AddAsync(group, cancellationToken);
        await modifierGroupRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(group);
    }

    public async Task<ModifierGroupResponse> UpdateModifierGroupAsync(
        Guid id,
        UpdateModifierGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        TranslationResolver.EnsurePrimaryLanguagePresent(
            request.Translations,
            t => t.LanguageCode,
            primaryLanguage,
            "modifier group");

        var groupTranslations = request.Translations
            .Select(t => (t.LanguageCode, t.Name));

        var modifiers = request.Modifiers
            .Select(m => (
                m.PriceAdjustment,
                m.SortOrder,
                (IEnumerable<(string languageCode, string name)>)m.Translations
                    .Select(t => (t.LanguageCode, t.Name))));

        var group = await modifierGroupRepository.UpdateAsync(
            id,
            g => g.Update(groupTranslations, modifiers),
            cancellationToken);

        if (group is null)
            throw new KeyNotFoundException($"ModifierGroup with id '{id}' was not found.");

        return MapToResponse(group);
    }

    public async Task DeleteModifierGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var deleted = await modifierGroupRepository.SoftDeleteAsync(id, cancellationToken);
        if (!deleted)
            throw new KeyNotFoundException($"ModifierGroup with id '{id}' was not found.");
    }

    public async Task<ModifierGroupResponse> GetModifierGroupAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await modifierGroupRepository.GetByIdAsync(id, cancellationToken);
        if (group is null)
            throw new KeyNotFoundException($"ModifierGroup with id '{id}' was not found.");

        return MapToResponse(group);
    }

    public async Task<IReadOnlyList<ModifierGroupListItemResponse>> GetModifierGroupsAsync(
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);

        var groups = await modifierGroupRepository.GetAllAsync(cancellationToken);
        return groups.Select(g => MapToListItem(g, primaryLanguage)).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ProductModifierGroupResponse>> GetProductModifierGroupsAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var primaryLanguage = await GetPrimaryLanguageAsync(cancellationToken);
        var assignments = await modifierGroupRepository.GetProductModifierGroupsAsync(productId, cancellationToken);

        var result = new List<ProductModifierGroupResponse>(assignments.Count);
        foreach (var pmg in assignments)
        {
            // The assignment only stores the group id + sort order; load the full group
            // so the response carries the resolved name + modifiers the menu UI needs.
            var group = await modifierGroupRepository.GetByIdAsync(pmg.ModifierGroupId, cancellationToken);
            if (group is null)
                continue; // assigned group was soft-deleted — skip rather than emit a broken row

            var name = TranslationResolver.ResolveName(
                group.Translations, t => t.LanguageCode, t => t.Name, primaryLanguage);

            var modifiers = group.Modifiers
                .OrderBy(m => m.SortOrder)
                .Select(m => new ModifierResponse(
                    m.Id,
                    m.PriceAdjustment,
                    m.SortOrder,
                    m.Translations
                        .Select(t => new ModifierTranslationResponse(t.LanguageCode, t.Name))
                        .ToList().AsReadOnly()))
                .ToList().AsReadOnly();

            result.Add(new ProductModifierGroupResponse(
                pmg.Id, pmg.ProductId, pmg.ModifierGroupId, name, pmg.SortOrder, modifiers));
        }

        return result.AsReadOnly();
    }

    public async Task SetProductModifierGroupsAsync(
        Guid productId,
        SetProductModifierGroupsRequest request,
        CancellationToken cancellationToken = default)
    {
        var assignments = request.Assignments
            .Select(a => (a.ModifierGroupId, a.SortOrder));

        await modifierGroupRepository.SetProductModifierGroupsAsync(productId, assignments, cancellationToken);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static ModifierGroupResponse MapToResponse(ModifierGroup group) =>
        new(
            group.Id,
            group.Translations
                .Select(t => new GroupTranslationResponse(t.LanguageCode, t.Name))
                .ToList().AsReadOnly(),
            group.Modifiers
                .OrderBy(m => m.SortOrder)
                .Select(m => new ModifierResponse(
                    m.Id,
                    m.PriceAdjustment,
                    m.SortOrder,
                    m.Translations
                        .Select(t => new ModifierTranslationResponse(t.LanguageCode, t.Name))
                        .ToList().AsReadOnly()))
                .ToList().AsReadOnly(),
            group.CreatedAt,
            group.UpdatedAt);

    private static ModifierGroupListItemResponse MapToListItem(ModifierGroup group, string primaryLanguage) =>
        new(
            group.Id,
            TranslationResolver.ResolveName(
                group.Translations,
                t => t.LanguageCode,
                t => t.Name,
                primaryLanguage),
            group.Modifiers.Count,
            group.CreatedAt);
}
