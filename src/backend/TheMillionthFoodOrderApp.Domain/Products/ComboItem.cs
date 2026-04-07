using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Products;

public sealed class ComboItem : Entity<Guid>
{
    public Guid ComboProductId { get; private set; }
    public Guid ComponentProductId { get; private set; }
    public int SortOrder { get; private set; }

    private ComboItem() { } // EF Core

    public static ComboItem Create(Guid comboProductId, Guid componentProductId, int sortOrder)
    {
        return new ComboItem
        {
            Id = Guid.CreateVersion7(),
            ComboProductId = comboProductId,
            ComponentProductId = componentProductId,
            SortOrder = sortOrder,
        };
    }
}
