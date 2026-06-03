using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Orders;

/// <summary>
/// The Order aggregate root. Represents a customer's food order placed at a shop.
/// Persisted in the brand database (database-per-brand isolation).
/// </summary>
public sealed class Order : AggregateRoot<Guid>, IAuditable
{
    public Guid ShopId { get; private set; }
    public string BrandSlug { get; private set; } = string.Empty;

    /// <summary>Short, human-readable order identifier (unique within a shop).</summary>
    public string OrderNumber { get; private set; } = string.Empty;

    public OrderType OrderType { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>Name of the status the order was placed in (opening lifecycle status).</summary>
    public string StatusName { get; private set; } = string.Empty;

    /// <summary>Optional customer name for display on kitchen displays and receipts.</summary>
    public string? CustomerName { get; private set; }

    /// <summary>Optional customer email address for digital receipts (US-FP-017).</summary>
    public string? CustomerEmail { get; private set; }

    /// <summary>Optional customer phone number (US-FP-017).</summary>
    public string? CustomerPhone { get; private set; }

    /// <summary>
    /// Table number for eat-in orders placed by counter staff.
    /// Null for online/pickup/delivery orders.
    /// </summary>
    public int? TableNumber { get; private set; }

    /// <summary>
    /// The identity (sub claim) of the counter staff member who created this order.
    /// Null for customer-facing online orders. Set server-side from the authenticated user.
    /// </summary>
    public Guid? CreatedByStaffId { get; private set; }

    /// <summary>VAT rate applied to this order (6 for Pickup/Delivery, 21 for EatIn).</summary>
    public decimal VatRatePercent { get; private set; }

    /// <summary>Sum of all line totals (gross, VAT-inclusive).</summary>
    public decimal SubtotalGross { get; private set; }

    /// <summary>Total VAT amount across all items.</summary>
    public decimal TotalVatAmount { get; private set; }

    /// <summary>SubtotalGross minus TotalVatAmount.</summary>
    public decimal TotalNet { get; private set; }

    /// <summary>Total gross amount (== SubtotalGross for a simple order).</summary>
    public decimal TotalGross { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Required by EF Core
    private Order() { }

    /// <summary>
    /// Factory method — the only way to create a valid Order.
    /// Calculates aggregate totals from the provided items.
    /// Raises <see cref="OrderCreatedEvent"/> so it propagates via Wolverine/SignalR.
    /// </summary>
    /// <param name="orderId">
    /// Pre-generated order ID. Must be the same Guid used when creating the order items
    /// via <see cref="OrderItem.Create"/> so that the FK relationship is consistent.
    /// </param>
    /// <param name="tableNumber">
    /// Optional table number for eat-in in-store orders. When provided, must be greater than zero.
    /// </param>
    /// <param name="createdByStaffId">
    /// Optional staff member ID (from the authenticated user's sub claim). Set server-side only.
    /// </param>
    public static Order Create(
        Guid orderId,
        Guid shopId,
        string brandSlug,
        string orderNumber,
        OrderType orderType,
        PaymentMethod paymentMethod,
        string statusName,
        string? customerName,
        decimal vatRatePercent,
        IEnumerable<OrderItem> items,
        int? tableNumber = null,
        Guid? createdByStaffId = null,
        string? customerEmail = null,
        string? customerPhone = null)
    {
        if (tableNumber.HasValue && tableNumber.Value <= 0)
            throw new ArgumentException("TableNumber must be greater than zero when provided.", nameof(tableNumber));

        var itemList = items.ToList();
        if (itemList.Count == 0)
            throw new ArgumentException("An order must contain at least one item.", nameof(items));

        var subtotalGross = itemList.Sum(i => i.LineTotal);
        var totalVatAmount = itemList.Sum(i => i.UnitVatAmount * i.Quantity);
        var totalNet = subtotalGross - totalVatAmount;

        var now = DateTimeOffset.UtcNow;

        var order = new Order
        {
            Id = orderId,
            ShopId = shopId,
            BrandSlug = brandSlug,
            OrderNumber = orderNumber,
            OrderType = orderType,
            PaymentMethod = paymentMethod,
            StatusName = statusName,
            CustomerName = customerName,
            CustomerEmail = string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail.Trim(),
            CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone.Trim(),
            TableNumber = tableNumber,
            CreatedByStaffId = createdByStaffId,
            VatRatePercent = vatRatePercent,
            SubtotalGross = Math.Round(subtotalGross, 2, MidpointRounding.AwayFromZero),
            TotalVatAmount = Math.Round(totalVatAmount, 2, MidpointRounding.AwayFromZero),
            TotalNet = Math.Round(totalNet, 2, MidpointRounding.AwayFromZero),
            TotalGross = Math.Round(subtotalGross, 2, MidpointRounding.AwayFromZero),
            CreatedAt = now,
            UpdatedAt = now,
        };

        // Items reference the order's Id — must be set after the order is created
        order._items.AddRange(itemList);

        order.AddDomainEvent(new OrderCreatedEvent(
            order.Id,
            shopId,
            brandSlug,
            orderNumber,
            statusName,
            customerName));

        return order;
    }
}
