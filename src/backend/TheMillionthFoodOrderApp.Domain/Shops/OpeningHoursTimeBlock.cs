using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Shops;

/// <summary>
/// A weekly recurring time block during which a shop is open.
/// Child entity of <see cref="Shop"/> — always accessed through the aggregate.
/// </summary>
public sealed class OpeningHoursTimeBlock : Entity<Guid>
{
    public Guid ShopId { get; private set; }
    public DayOfWeek DayOfWeek { get; private set; }
    public TimeOnly OpenTime { get; private set; }
    public TimeOnly CloseTime { get; private set; }

    // Required by EF Core
    private OpeningHoursTimeBlock() { }

    /// <summary>
    /// Factory method — the only way to create a valid <see cref="OpeningHoursTimeBlock"/>.
    /// </summary>
    /// <param name="shopId">The owning shop's identifier.</param>
    /// <param name="day">Day of the week this block applies to.</param>
    /// <param name="open">Opening time (local to the shop's time zone).</param>
    /// <param name="close">Closing time (local to the shop's time zone). Must be after <paramref name="open"/>.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="close"/> is not after <paramref name="open"/>.</exception>
    public static OpeningHoursTimeBlock Create(Guid shopId, DayOfWeek day, TimeOnly open, TimeOnly close)
    {
        if (close <= open)
            throw new ArgumentException(
                $"CloseTime ({close}) must be after OpenTime ({open}). Overnight blocks are not supported.",
                nameof(close));

        return new OpeningHoursTimeBlock
        {
            Id = Guid.CreateVersion7(),
            ShopId = shopId,
            DayOfWeek = day,
            OpenTime = open,
            CloseTime = close,
        };
    }
}
