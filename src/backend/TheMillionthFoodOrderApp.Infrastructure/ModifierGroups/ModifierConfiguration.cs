using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;

namespace TheMillionthFoodOrderApp.Infrastructure.ModifierGroups;

public sealed class ModifierConfiguration : IEntityTypeConfiguration<Modifier>
{
    public void Configure(EntityTypeBuilder<Modifier> builder)
    {
        builder.ToTable("Modifiers");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.PriceAdjustment)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(m => m.SortOrder)
            .IsRequired();

        // EF Core shadow property for FK to ModifierGroup
        builder.Property<Guid>("ModifierGroupId")
            .IsRequired();

        builder.HasMany(m => m.Translations)
            .WithOne()
            .HasForeignKey(t => t.ModifierId)
            .OnDelete(DeleteBehavior.Cascade);

        // Domain events are transient — never persisted
        builder.Ignore(m => m.DomainEvents);
    }
}
