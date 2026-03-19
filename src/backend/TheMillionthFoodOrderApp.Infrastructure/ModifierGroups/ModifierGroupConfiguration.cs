using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;

namespace TheMillionthFoodOrderApp.Infrastructure.ModifierGroups;

public sealed class ModifierGroupConfiguration : IEntityTypeConfiguration<ModifierGroup>
{
    public void Configure(EntityTypeBuilder<ModifierGroup> builder)
    {
        builder.ToTable("ModifierGroups");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.IsDeleted)
            .IsRequired();

        builder.Property(g => g.CreatedAt)
            .IsRequired();

        builder.Property(g => g.UpdatedAt)
            .IsRequired();

        builder.HasMany(g => g.Translations)
            .WithOne()
            .HasForeignKey(t => t.ModifierGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(g => g.Modifiers)
            .WithOne()
            .HasForeignKey("ModifierGroupId")
            .OnDelete(DeleteBehavior.Cascade);

        // Domain events are transient — never persisted
        builder.Ignore(g => g.DomainEvents);
    }
}
