using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.ModifierGroups;

public sealed class ModifierTranslation : Entity<Guid>
{
    public Guid ModifierId { get; private set; }
    public string LanguageCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    private ModifierTranslation() { } // EF Core

    public static ModifierTranslation Create(Guid modifierId, string languageCode, string name)
    {
        return new ModifierTranslation
        {
            Id = Guid.CreateVersion7(),
            ModifierId = modifierId,
            LanguageCode = languageCode,
            Name = name,
        };
    }
}
