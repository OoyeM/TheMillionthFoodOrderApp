using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.ModifierGroups;

/// <summary>
/// Join entity between Product and ModifierGroup.
/// SortOrder controls the display order of this group on the product's detail page.
/// </summary>
public sealed class ProductModifierGroup : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public Guid ModifierGroupId { get; private set; }

    /// <summary>
    /// Display order of this modifier group on the product. 0-based, ascending.
    /// </summary>
    public int SortOrder { get; private set; }

    private ProductModifierGroup() { } // EF Core

    public static ProductModifierGroup Create(Guid productId, Guid modifierGroupId, int sortOrder)
    {
        return new ProductModifierGroup
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            ModifierGroupId = modifierGroupId,
            SortOrder = sortOrder,
        };
    }
}
