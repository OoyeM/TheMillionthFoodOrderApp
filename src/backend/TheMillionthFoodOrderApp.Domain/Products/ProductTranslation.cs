using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Products;

public sealed class ProductTranslation : Entity<Guid>
{
    public Guid ProductId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private ProductTranslation() { } // EF Core

    public static ProductTranslation Create(Guid productId, string languageCode, string name, string? description)
    {
        return new ProductTranslation
        {
            Id = Guid.CreateVersion7(),
            ProductId = productId,
            LanguageCode = languageCode,
            Name = name,
            Description = description,
        };
    }
}
