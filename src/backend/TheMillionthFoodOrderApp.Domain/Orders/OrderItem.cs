using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Orders;

/// <summary>
/// A line item in an order, representing one product (possibly with modifier selections).
/// Prices are denormalised at creation time so they remain accurate if the product changes later.
/// </summary>
public sealed class OrderItem : Entity<Guid>
{
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }

    /// <summary>Denormalised product name captured at order time.</summary>
    public string ProductName { get; private set; } = string.Empty;

    public int Quantity { get; private set; }

    /// <summary>Gross (VAT-inclusive) unit price at order time (base product price + modifier adjustments).</summary>
    public decimal UnitGrossPrice { get; private set; }

    /// <summary>Net (excl. VAT) unit price derived from UnitGrossPrice at order time.</summary>
    public decimal UnitNetPrice { get; private set; }

    /// <summary>VAT amount per unit.</summary>
    public decimal UnitVatAmount { get; private set; }

    /// <summary>Total line gross = UnitGrossPrice * Quantity (including modifier price adjustments).</summary>
    public decimal LineTotal { get; private set; }

    private readonly List<SelectedModifier> _selectedModifiers = [];
    public IReadOnlyCollection<SelectedModifier> SelectedModifiers => _selectedModifiers.AsReadOnly();

    // Required by EF Core
    private OrderItem() { }

    /// <summary>
    /// Factory method. Calculates line total from the provided gross breakdown.
    /// <para>
    /// <paramref name="unitGrossPrice"/> must already include all modifier price adjustments
    /// (combined gross = base price + sum of modifier PriceAdjustments). VAT decomposition
    /// (net/vat) is derived from this combined amount by the caller before invoking Create.
    /// </para>
    /// </summary>
    public static OrderItem Create(
        Guid orderId,
        Guid productId,
        string productName,
        int quantity,
        decimal unitGrossPrice,
        decimal unitNetPrice,
        decimal unitVatAmount,
        IEnumerable<SelectedModifier> selectedModifiers)
    {
        var modifierList = selectedModifiers.ToList();

        var item = new OrderItem
        {
            Id = Guid.CreateVersion7(),
            OrderId = orderId,
            ProductId = productId,
            ProductName = productName,
            Quantity = quantity,
            UnitGrossPrice = unitGrossPrice,
            UnitNetPrice = unitNetPrice,
            UnitVatAmount = unitVatAmount,
            // LineTotal = combined unit gross (base + modifiers) × quantity.
            // unitGrossPrice already incorporates modifier adjustments, so no further addition needed.
            LineTotal = unitGrossPrice * quantity,
        };

        item._selectedModifiers.AddRange(modifierList);

        return item;
    }
}
