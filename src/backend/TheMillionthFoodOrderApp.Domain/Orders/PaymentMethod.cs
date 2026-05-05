namespace TheMillionthFoodOrderApp.Domain.Orders;

/// <summary>
/// Describes how the customer intends to pay for their order.
/// Designed as a seam: CashAtPickup is handled in-process;
/// CreditCard and Bancontact will route through a payment provider (e.g. Mollie/Stripe) in a future story.
/// </summary>
public enum PaymentMethod
{
    /// <summary>The customer pays in cash when collecting their order at the counter.</summary>
    CashAtPickup = 0,

    /// <summary>The customer pays by credit card (online or terminal). Requires a payment provider integration.</summary>
    CreditCard = 1,

    /// <summary>The customer pays via Bancontact (Belgian debit network). Requires a payment provider integration.</summary>
    Bancontact = 2,
}
