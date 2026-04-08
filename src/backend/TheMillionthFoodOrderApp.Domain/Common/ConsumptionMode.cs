namespace TheMillionthFoodOrderApp.Domain.Common;

/// <summary>
/// Describes how food is consumed, which determines the applicable Belgian VAT rate.
/// Belgian legislation distinguishes between takeaway and eat-in consumption for food VAT purposes.
/// </summary>
public enum ConsumptionMode
{
    /// <summary>
    /// The order is taken away from the premises.
    /// In Belgium this attracts the reduced food VAT rate of 6%.
    /// </summary>
    Takeaway = 0,

    /// <summary>
    /// The order is consumed on the premises (dine-in / eat-in).
    /// In Belgium this attracts the standard VAT rate of 21% (restaurant services).
    /// </summary>
    EatIn = 1,
}
