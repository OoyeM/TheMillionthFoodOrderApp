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

    /// <summary>Optional customer first name (US-FP-051).</summary>
    public string? CustomerFirstName { get; private set; }

    /// <summary>Optional customer last name (US-FP-051).</summary>
    public string? CustomerLastName { get; private set; }

    /// <summary>
    /// Combined customer name for display on kitchen displays, tickets and receipts.
    /// Computed from <see cref="CustomerFirstName"/> + <see cref="CustomerLastName"/>; not persisted.
    /// </summary>
    public string? CustomerName
    {
        get
        {
            var combined = string.Join(' ', new[] { CustomerFirstName, CustomerLastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            return string.IsNullOrWhiteSpace(combined) ? null : combined;
        }
    }

    /// <summary>Optional customer email address for digital receipts (US-FP-017).</summary>
    public string? CustomerEmail { get; private set; }

    /// <summary>Optional customer phone number (US-FP-017).</summary>
    public string? CustomerPhone { get; private set; }

    /// <summary>
    /// BCP-47 language code of the customer's checkout language (e.g. "nl-BE"),
    /// used to render the digital receipt email in their language (US-FP-051).
    /// Defaults to the brand's primary language.
    /// </summary>
    public string LanguageCode { get; private set; } = "nl-BE";

    /// <summary>
    /// UTC start of the selected time slot (US-FP-019). Null means "as soon as possible".
    /// When set, <see cref="TimeSlotEnd"/> is also set.
    /// </summary>
    public DateTimeOffset? TimeSlotStart { get; private set; }

    /// <summary>
    /// UTC end of the selected time slot (US-FP-019). Null means "as soon as possible".
    /// Stored to denormalise the slot boundary so future admin config changes
    /// (interval/capacity) do not corrupt order history.
    /// </summary>
    public DateTimeOffset? TimeSlotEnd { get; private set; }

    /// <summary>
    /// True once a digital receipt email has been sent for this order (US-FP-051).
    /// Guards against sending duplicate receipts on repeated terminal transitions.
    /// </summary>
    public bool ReceiptEmailSent { get; private set; }

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
        string? customerFirstName,
        string? customerLastName,
        decimal vatRatePercent,
        IEnumerable<OrderItem> items,
        int? tableNumber = null,
        Guid? createdByStaffId = null,
        string? customerEmail = null,
        string? customerPhone = null,
        string? languageCode = null,
        DateTimeOffset? timeSlotStart = null,
        DateTimeOffset? timeSlotEnd = null)
    {
        if (tableNumber.HasValue && tableNumber.Value <= 0)
            throw new ArgumentException("TableNumber must be greater than zero when provided.", nameof(tableNumber));

        // Time-slot invariants: both-or-neither, and end must be after start (US-FP-019).
        if (timeSlotStart.HasValue != timeSlotEnd.HasValue)
            throw new ArgumentException("TimeSlotStart and TimeSlotEnd must both be set or both be null.");
        if (timeSlotStart.HasValue && timeSlotEnd.HasValue && timeSlotEnd.Value <= timeSlotStart.Value)
            throw new ArgumentException("TimeSlotEnd must be after TimeSlotStart.");

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
            CustomerFirstName = string.IsNullOrWhiteSpace(customerFirstName) ? null : customerFirstName.Trim(),
            CustomerLastName = string.IsNullOrWhiteSpace(customerLastName) ? null : customerLastName.Trim(),
            CustomerEmail = string.IsNullOrWhiteSpace(customerEmail) ? null : customerEmail.Trim(),
            CustomerPhone = string.IsNullOrWhiteSpace(customerPhone) ? null : customerPhone.Trim(),
            LanguageCode = string.IsNullOrWhiteSpace(languageCode) ? "nl-BE" : languageCode.Trim(),
            TableNumber = tableNumber,
            CreatedByStaffId = createdByStaffId,
            TimeSlotStart = timeSlotStart,
            TimeSlotEnd = timeSlotEnd,
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
            order.CustomerName));

        return order;
    }

    /// <summary>
    /// Advances the order to a new lifecycle status (US-FP-023).
    /// Raises <see cref="OrderStatusChangedEvent"/> so the change propagates via
    /// Wolverine/SignalR to the kitchen display, POS, and the customer's tracking page.
    /// </summary>
    /// <remarks>
    /// Whether the transition is <em>allowed</em> (i.e. configured in the shop's
    /// <c>OrderLifecycleConfig</c>) is a cross-aggregate concern enforced by the
    /// application service before this method is called. The aggregate only guards
    /// the invariants it owns: a non-empty, genuinely different status name.
    /// </remarks>
    public void AdvanceTo(string newStatusName)
    {
        if (string.IsNullOrWhiteSpace(newStatusName))
            throw new ArgumentException("Status name is required.", nameof(newStatusName));

        if (string.Equals(newStatusName, StatusName, StringComparison.Ordinal))
            throw new InvalidOperationException($"Order is already in status '{StatusName}'.");

        var previousStatus = StatusName;
        StatusName = newStatusName;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new OrderStatusChangedEvent(
            Id,
            ShopId,
            BrandSlug,
            previousStatus,
            newStatusName,
            CustomerName));
    }

    /// <summary>
    /// Marks that a digital receipt email has been sent for this order (US-FP-051).
    /// Idempotent: a no-op once already set, so repeated terminal transitions
    /// never produce duplicate receipts.
    /// </summary>
    public void MarkReceiptEmailSent()
    {
        if (ReceiptEmailSent)
            return;

        ReceiptEmailSent = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
