namespace TheMillionthFoodOrderApp.Application.ModifierGroups;

// ── Shared ───────────────────────────────────────────────────────────────────

public sealed record ModifierTranslationRequest(string LanguageCode, string Name);
public sealed record ModifierTranslationResponse(string LanguageCode, string Name);

public sealed record GroupTranslationRequest(string LanguageCode, string Name);
public sealed record GroupTranslationResponse(string LanguageCode, string Name);

// ── Modifier DTO ─────────────────────────────────────────────────────────────

public sealed record ModifierRequest(
    decimal PriceAdjustment,
    int SortOrder,
    IReadOnlyList<ModifierTranslationRequest> Translations);

public sealed record ModifierResponse(
    Guid Id,
    decimal PriceAdjustment,
    int SortOrder,
    IReadOnlyList<ModifierTranslationResponse> Translations);

// ── ModifierGroup Create / Update ─────────────────────────────────────────────

public sealed record CreateModifierGroupRequest(
    IReadOnlyList<GroupTranslationRequest> Translations,
    IReadOnlyList<ModifierRequest> Modifiers);

public sealed record UpdateModifierGroupRequest(
    IReadOnlyList<GroupTranslationRequest> Translations,
    IReadOnlyList<ModifierRequest> Modifiers);

// ── ModifierGroup Responses ───────────────────────────────────────────────────

public sealed record ModifierGroupResponse(
    Guid Id,
    IReadOnlyList<GroupTranslationResponse> Translations,
    IReadOnlyList<ModifierResponse> Modifiers,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ModifierGroupListItemResponse(
    Guid Id,
    string Name,
    int ModifierCount,
    DateTimeOffset CreatedAt);

// ── Product ↔ ModifierGroup assignment ───────────────────────────────────────

public sealed record ProductModifierGroupAssignment(Guid ModifierGroupId, int SortOrder);

public sealed record ProductModifierGroupResponse(
    Guid Id,
    Guid ProductId,
    Guid ModifierGroupId,
    int SortOrder);

public sealed record SetProductModifierGroupsRequest(
    IReadOnlyList<ProductModifierGroupAssignment> Assignments);
