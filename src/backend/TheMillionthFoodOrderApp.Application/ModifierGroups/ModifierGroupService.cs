using TheMillionthFoodOrderApp.Domain.ModifierGroups;

namespace TheMillionthFoodOrderApp.Application.ModifierGroups;

public sealed class ModifierGroupService(IModifierGroupRepository modifierGroupRepository) : IModifierGroupService
{
    public async Task<ModifierGroupResponse> CreateModifierGroupAsync(
        CreateModifierGroupRequest request,
        CancellationToken cancellationToken = default)
    {
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
        var group = await modifierGroupRepository.UpdateAsync(
            id, g => g.SoftDelete(), cancellationToken);

        if (group is null)
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
        var groups = await modifierGroupRepository.GetAllAsync(cancellationToken);
        return groups.Select(MapToListItem).ToList().AsReadOnly();
    }

    public async Task<IReadOnlyList<ProductModifierGroupResponse>> GetProductModifierGroupsAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
    {
        var assignments = await modifierGroupRepository.GetProductModifierGroupsAsync(productId, cancellationToken);
        return assignments
            .Select(pmg => new ProductModifierGroupResponse(pmg.Id, pmg.ProductId, pmg.ModifierGroupId, pmg.SortOrder))
            .ToList()
            .AsReadOnly();
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

    private static ModifierGroupListItemResponse MapToListItem(ModifierGroup group) =>
        new(
            group.Id,
            group.Translations.FirstOrDefault()?.Name ?? "(unnamed)",
            group.Modifiers.Count,
            group.CreatedAt);
}
