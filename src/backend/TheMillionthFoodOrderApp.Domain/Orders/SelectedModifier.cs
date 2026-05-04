namespace TheMillionthFoodOrderApp.Domain.Orders;

/// <summary>
/// Denormalised snapshot of a modifier selection captured at order time.
/// Stored as a value object inside OrderItem — modifier name and price adjustment
/// are copied at creation so they remain accurate even if the modifier is later changed.
/// </summary>
public sealed class SelectedModifier
{
    public Guid ModifierId { get; private set; }

    /// <summary>Denormalised modifier name at the time of ordering.</summary>
    public string ModifierName { get; private set; } = string.Empty;

    /// <summary>Price delta applied by this modifier (can be negative or zero).</summary>
    public decimal PriceAdjustment { get; private set; }

    // Required by EF Core
    private SelectedModifier() { }

    public static SelectedModifier Create(Guid modifierId, string modifierName, decimal priceAdjustment) =>
        new()
        {
            ModifierId = modifierId,
            ModifierName = modifierName,
            PriceAdjustment = priceAdjustment,
        };
}
