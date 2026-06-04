using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Shops;

/// <summary>
/// Per-shop eat-in ordering configuration (US-FP-066).
/// When <see cref="IsEnabled"/> is false, eat-in order types and table-number entry are
/// hidden from both the storefront and the in-store interface, and the API rejects eat-in
/// orders for the shop. When enabled, <see cref="RequiresTableNumber"/> controls whether a
/// table number is mandatory for eat-in orders.
/// </summary>
public sealed class EatInSettings : ValueObject
{
    /// <summary>Whether the shop accepts eat-in orders.</summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// When true (and <see cref="IsEnabled"/> is true), a table number is mandatory for
    /// eat-in orders. Has no effect when eat-in is disabled.
    /// </summary>
    public bool RequiresTableNumber { get; }

    // Required by EF Core owned-entity materialisation.
    private EatInSettings() { }

    public EatInSettings(bool isEnabled, bool requiresTableNumber)
    {
        IsEnabled = isEnabled;
        RequiresTableNumber = requiresTableNumber;
    }

    /// <summary>Default for newly-created shops: eat-in enabled, table number required.</summary>
    public static EatInSettings CreateDefault() => new(isEnabled: true, requiresTableNumber: true);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return RequiresTableNumber;
    }
}
