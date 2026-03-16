using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.MenuCategories;

public sealed class MenuCategoryTranslation : Entity<Guid>
{
    public Guid MenuCategoryId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    private MenuCategoryTranslation() { } // EF Core

    public static MenuCategoryTranslation Create(Guid menuCategoryId, string languageCode, string name, string? description)
    {
        return new MenuCategoryTranslation
        {
            Id = Guid.CreateVersion7(),
            MenuCategoryId = menuCategoryId,
            LanguageCode = languageCode,
            Name = name,
            Description = description,
        };
    }
}
