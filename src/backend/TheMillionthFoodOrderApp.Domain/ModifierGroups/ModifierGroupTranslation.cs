using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.ModifierGroups;

public sealed class ModifierGroupTranslation : Entity<Guid>
{
    public Guid ModifierGroupId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private ModifierGroupTranslation() { } // EF Core

    public static ModifierGroupTranslation Create(Guid modifierGroupId, string languageCode, string name)
    {
        return new ModifierGroupTranslation
        {
            Id = Guid.CreateVersion7(),
            ModifierGroupId = modifierGroupId,
            LanguageCode = languageCode,
            Name = name,
        };
    }
}
