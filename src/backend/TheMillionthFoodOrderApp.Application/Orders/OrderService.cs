using TheMillionthFoodOrderApp.Application.Orders.Dtos;
using TheMillionthFoodOrderApp.Domain.Common;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;
using TheMillionthFoodOrderApp.Domain.Orders;
using TheMillionthFoodOrderApp.Domain.Products;
using TheMillionthFoodOrderApp.Domain.Shops;
using TheMillionthFoodOrderApp.Domain.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Application.Orders;

/// <summary>
/// Application service for placing orders.
/// Resolves prices and modifier details from the database,
/// applies the correct Belgian VAT rate based on order type,
/// and persists the order aggregate.
/// </summary>
public sealed class OrderService(
    IOrderRepository orderRepository,
    IProductRepository productRepository,
    IModifierGroupRepository modifierGroupRepository,
    ITaxConfigurationRepository taxConfigurationRepository,
    IOrderLifecycleConfigRepository orderLifecycleConfigRepository,
    IShopRepository shopRepository) : IOrderService
{
    public async Task<OrderResponse> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        // 1. Parse and validate order type
        if (!Enum.TryParse<OrderType>(request.OrderType, ignoreCase: true, out var orderType) || !Enum.IsDefined(orderType))
            throw new ArgumentException(
                $"Invalid order type: '{request.OrderType}'. Valid values: {string.Join(", ", Enum.GetNames<OrderType>())}.");

        // 1b. Parse and validate payment method
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var paymentMethod) || !Enum.IsDefined(paymentMethod))
            throw new ArgumentException(
                $"Invalid payment method: '{request.PaymentMethod}'. Valid values: {string.Join(", ", Enum.GetNames<PaymentMethod>())}.");

        if (request.Items.Count == 0)
            throw new ArgumentException("An order must contain at least one item.");

        return await CreateOrderCoreAsync(
            request.ShopId,
            request.BrandSlug,
            orderType,
            paymentMethod,
            request.CustomerName,
            tableNumber: null,
            createdByStaffId: null,
            request.Items,
            cancellationToken,
            request.CustomerEmail,
            request.CustomerPhone,
            enforceOpeningHours: true);
    }

    public async Task<OrderResponse> CreateInStoreOrderAsync(
        CreateInStoreOrderRequest request,
        Guid? createdByStaffId,
        CancellationToken cancellationToken = default)
    {
        // 1. Parse and validate order type
        if (!Enum.TryParse<OrderType>(request.OrderType, ignoreCase: true, out var orderType) || !Enum.IsDefined(orderType))
            throw new ArgumentException(
                $"Invalid order type: '{request.OrderType}'. Valid values: {string.Join(", ", Enum.GetNames<OrderType>())}.");

        // 1b. Parse and validate payment method (for enum validity only — will be forced to CashAtPickup)
        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out _) ||
            !Enum.IsDefined(Enum.Parse<PaymentMethod>(request.PaymentMethod, ignoreCase: true)))
            throw new ArgumentException(
                $"Invalid payment method: '{request.PaymentMethod}'. Valid values: {string.Join(", ", Enum.GetNames<PaymentMethod>())}.");

        if (request.Items.Count == 0)
            throw new ArgumentException("An order must contain at least one item.");

        // 2. EatIn requires a valid table number
        if (orderType == OrderType.EatIn)
        {
            if (request.TableNumber is null)
                throw new ArgumentException("TableNumber is required for EatIn orders.");
            if (request.TableNumber.Value <= 0)
                throw new ArgumentException("TableNumber must be greater than zero.");
        }

        // 3. Force payment method to CashAtPickup for in-store orders
        var paymentMethod = PaymentMethod.CashAtPickup;

        // 4. createdByStaffId is supplied by the caller (endpoint extracts it from claims)
        //    and is never read from the request DTO to prevent client-trust issues.
        return await CreateOrderCoreAsync(
            request.ShopId,
            request.BrandSlug,
            orderType,
            paymentMethod,
            request.CustomerName,
            request.TableNumber,
            createdByStaffId,
            request.Items,
            cancellationToken);
    }

    public async Task<OrderResponse> AdvanceOrderStatusAsync(
        Guid shopId,
        Guid orderId,
        Guid toStatusId,
        CancellationToken cancellationToken = default)
    {
        // 1. Load the order and confirm it belongs to the shop named in the route.
        var order = await orderRepository.GetByIdAsync(orderId, cancellationToken);
        if (order is null || order.ShopId != shopId)
            throw new KeyNotFoundException(
                $"Order with id '{orderId}' was not found for shop '{shopId}'.");

        // 2. Load the shop's lifecycle config (statuses + allowed transitions).
        var lifecycleConfig = await orderLifecycleConfigRepository.GetByShopIdAsync(shopId, cancellationToken)
            ?? throw new InvalidOperationException(
                "This shop has no order lifecycle configuration.");

        // 3. Resolve the current status by name (orders store the denormalised status name).
        var currentStatus = lifecycleConfig.Statuses.FirstOrDefault(s => s.Name == order.StatusName)
            ?? throw new InvalidOperationException(
                $"The order's current status '{order.StatusName}' is not part of the shop's lifecycle.");

        // 4. Resolve the requested target status.
        var targetStatus = lifecycleConfig.Statuses.FirstOrDefault(s => s.Id == toStatusId)
            ?? throw new KeyNotFoundException(
                $"Status with id '{toStatusId}' was not found in the shop's lifecycle.");

        // 5. The transition must be explicitly configured (current → target).
        var transitionAllowed = lifecycleConfig.Transitions.Any(
            t => t.FromStatusId == currentStatus.Id && t.ToStatusId == targetStatus.Id);
        if (!transitionAllowed)
            throw new InvalidOperationException(
                $"Cannot advance order from '{currentStatus.Name}' to '{targetStatus.Name}': " +
                "no such transition is configured for this shop.");

        // 6. Apply the change (raises OrderStatusChangedEvent) and persist —
        //    SaveChangesAsync dispatches the event via Wolverine → SignalR.
        order.AdvanceTo(targetStatus.Name);
        await orderRepository.SaveChangesAsync(cancellationToken);

        // The status-advance response feeds the kitchen display, which renders an order
        // ticket (not a customer receipt), so the seller legal block is not needed here.
        return MapToResponse(order, shop: null);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Shared order-creation core: resolves VAT, shop, lifecycle, products, modifiers,
    /// builds order items, and persists with retry logic. Both the public and in-store
    /// endpoints delegate here so pricing/VAT/modifier logic is never duplicated.
    /// </summary>
    private async Task<OrderResponse> CreateOrderCoreAsync(
        Guid shopId,
        string brandSlug,
        OrderType orderType,
        PaymentMethod paymentMethod,
        string? customerName,
        int? tableNumber,
        Guid? createdByStaffId,
        IReadOnlyList<OrderItemInput> items,
        CancellationToken cancellationToken,
        string? customerEmail = null,
        string? customerPhone = null,
        bool enforceOpeningHours = false)
    {
        // 1. Determine consumption mode for VAT calculation
        var consumptionMode = orderType == OrderType.EatIn
            ? ConsumptionMode.EatIn
            : ConsumptionMode.Takeaway;

        // 2. Load tax configuration and resolve VAT rate
        var taxConfig = await taxConfigurationRepository.GetAsync(cancellationToken)
            ?? throw new InvalidOperationException("No tax configuration has been set up for this brand.");

        var vatRate = taxConfig.GetRateForMode(consumptionMode);

        // 3. Validate that the shop exists in this brand's database
        var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken);
        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{shopId}' was not found.");

        // 3b. Online customer orders are rejected when the shop is currently closed (US-FP-071 / #127).
        //     In-store staff orders are exempt — staff are physically present at the counter.
        if (enforceOpeningHours && !shop.IsOpenAt(DateTimeOffset.UtcNow))
            throw new InvalidOperationException(
                "This shop is currently closed and is not accepting online orders.");

        // 4. Load order lifecycle config to determine opening status (lazy-init default if missing)
        var lifecycleConfig = await orderLifecycleConfigRepository.GetByShopIdAsync(shopId, cancellationToken);
        if (lifecycleConfig is null)
        {
            lifecycleConfig = OrderLifecycleConfig.CreateDefault(shopId);
            await orderLifecycleConfigRepository.AddAsync(lifecycleConfig, cancellationToken);
            await orderLifecycleConfigRepository.SaveChangesAsync(cancellationToken);
        }

        var openingStatus = GetOpeningStatus(lifecycleConfig);

        // 5. Resolve products from DB (never trust client-submitted prices)
        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);

        if (products.Count != productIds.Count)
        {
            var missingIds = productIds.Except(products.Select(p => p.Id));
            throw new KeyNotFoundException(
                $"Product(s) not found: {string.Join(", ", missingIds)}.");
        }

        var productLookup = products.ToDictionary(p => p.Id);

        // 6. Resolve all requested modifier IDs
        var allModifierIds = items
            .SelectMany(i => i.SelectedModifierIds)
            .Distinct()
            .ToList();

        var modifierLookup = new Dictionary<Guid, Modifier>();
        if (allModifierIds.Count > 0)
        {
            // Load all modifier groups and build a flat modifier lookup
            // (Modifiers belong to groups; we query by modifier ID across all groups)
            var allGroups = await modifierGroupRepository.GetAllAsync(cancellationToken);
            modifierLookup = allGroups
                .SelectMany(g => g.Modifiers)
                .Where(m => allModifierIds.Contains(m.Id))
                .GroupBy(m => m.Id)
                .ToDictionary(g => g.Key, g => g.First());

            var missingModifierIds = allModifierIds.Except(modifierLookup.Keys).ToList();
            if (missingModifierIds.Count > 0)
                throw new KeyNotFoundException(
                    $"Modifier(s) not found: {string.Join(", ", missingModifierIds)}.");
        }

        // 7. Build order items with denormalised prices and modifiers
        // Pre-generate the order ID so item FKs and the order aggregate share the same value.
        var orderId = Guid.CreateVersion7();
        var orderItems = new List<OrderItem>();

        foreach (var itemInput in items)
        {
            var product = productLookup[itemInput.ProductId];

            // Build the selected modifier list first so we can sum their price adjustments
            var selectedModifiers = itemInput.SelectedModifierIds
                .Select(mid =>
                {
                    var mod = modifierLookup[mid];
                    var name = mod.Translations.FirstOrDefault()?.Name ?? "(unnamed)";
                    return SelectedModifier.Create(mid, name, mod.PriceAdjustment);
                })
                .ToList();

            // Combined unit gross = base price + sum of all modifier adjustments.
            // VAT is applied to the combined amount so the tax decomposition is accurate.
            var modifierTotal = selectedModifiers.Sum(m => m.PriceAdjustment);
            var combinedGrossPrice = product.BasePrice.Amount + modifierTotal;
            var taxBreakdown = TaxCalculator.CalculateFromGross(combinedGrossPrice, vatRate);

            var productName = product.Translations.FirstOrDefault()?.Name ?? "(unnamed)";

            var orderItem = OrderItem.Create(
                orderId,
                product.Id,
                productName,
                itemInput.Quantity,
                taxBreakdown.GrossAmount,
                taxBreakdown.NetAmount,
                taxBreakdown.VatAmount,
                selectedModifiers);

            orderItems.Add(orderItem);
        }

        // 8. Generate a unique order number and persist, retrying on the rare race condition
        //    where two concurrent requests pass the exists-check simultaneously and one
        //    hits the UX_Orders_ShopId_OrderNumber unique index on INSERT.
        //    OrderRepository.SaveChangesAsync detects the SQL unique-constraint error (2601/2627)
        //    and throws InvalidOperationException("ORDER_NUMBER_CONFLICT") so we can retry here
        //    without coupling the Application layer to EF Core.
        const int maxSaveAttempts = 5;
        Order? order = null;

        for (var saveAttempt = 0; saveAttempt < maxSaveAttempts; saveAttempt++)
        {
            var orderNumber = await GenerateUniqueOrderNumberAsync(shopId, cancellationToken);

            // 9. Create the order aggregate with the candidate number
            order = Order.Create(
                orderId,
                shopId,
                brandSlug,
                orderNumber,
                orderType,
                paymentMethod,
                openingStatus.Name,
                customerName,
                vatRate,
                orderItems,
                tableNumber,
                createdByStaffId,
                customerEmail,
                customerPhone);

            try
            {
                await orderRepository.AddAsync(order, cancellationToken);
                await orderRepository.SaveChangesAsync(cancellationToken);
                break; // success — exit retry loop
            }
            catch (InvalidOperationException ex) when (ex.Message == "ORDER_NUMBER_CONFLICT")
            {
                // The unique index was hit by a concurrent request. The repository has already
                // detached the failed entity. Re-generate a fresh ID and order number on next loop.
                orderId = Guid.CreateVersion7();
                if (saveAttempt == maxSaveAttempts - 1)
                    throw new InvalidOperationException(
                        "Failed to generate a unique order number after multiple retries. Please try again.", ex);
            }
        }

        return MapToResponse(order!, shop);
    }

    private static Domain.OrderLifecycle.OrderStatus GetOpeningStatus(OrderLifecycleConfig config)
    {
        // Opening status = lowest SortOrder; fall back to "placed" SystemKey if tied
        return config.Statuses
            .OrderBy(s => s.SortOrder)
            .First();
    }

    private async Task<string> GenerateUniqueOrderNumberAsync(
        Guid shopId,
        CancellationToken cancellationToken)
    {
        // Simple 8-char alphanumeric prefix from a UUIDv7 — retry on collision (rare)
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = Guid.CreateVersion7().ToString("N")[..8].ToUpperInvariant();
            var exists = await orderRepository.OrderNumberExistsAsync(shopId, candidate, cancellationToken);
            if (!exists)
                return candidate;
        }

        // Fallback: use full guid to guarantee uniqueness
        return Guid.CreateVersion7().ToString("N")[..16].ToUpperInvariant();
    }

    /// <summary>
    /// Maps an order to its response DTO. When <paramref name="shop"/> is supplied, the
    /// seller legal block (name, VAT number, address) is included for receipt rendering
    /// (US-FP-052); pass null on paths that do not render a receipt.
    /// </summary>
    private static OrderResponse MapToResponse(Order order, Shop? shop) =>
        new(
            order.Id,
            order.OrderNumber,
            order.ShopId,
            order.BrandSlug,
            order.OrderType.ToString(),
            order.PaymentMethod.ToString(),
            order.StatusName,
            order.CustomerName,
            order.Items
                .Select(i => new OrderItemResponse(
                    i.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitGrossPrice,
                    i.UnitNetPrice,
                    i.UnitVatAmount,
                    i.LineTotal,
                    i.SelectedModifiers
                        .Select(m => new SelectedModifierResponse(m.ModifierId, m.ModifierName, m.PriceAdjustment))
                        .ToList()
                        .AsReadOnly()))
                .ToList()
                .AsReadOnly(),
            order.VatRatePercent,
            order.SubtotalGross,
            order.TotalVatAmount,
            order.TotalNet,
            order.TotalGross,
            order.CreatedAt,
            order.TableNumber,
            order.CreatedByStaffId,
            order.CustomerEmail,
            order.CustomerPhone,
            shop?.Name,
            shop?.VatNumber,
            shop?.Address.ToSingleLine());
}
