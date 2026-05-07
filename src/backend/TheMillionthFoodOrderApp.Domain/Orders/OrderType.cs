namespace TheMillionthFoodOrderApp.Domain.Orders;

/// <summary>
/// Describes how an order will be fulfilled.
/// </summary>
public enum OrderType
{
    /// <summary>The customer picks up their order at the counter.</summary>
    Pickup = 0,

    /// <summary>The customer eats their order on the premises.</summary>
    EatIn = 1,

    /// <summary>The order is delivered to the customer's address.</summary>
    Delivery = 2,
}
