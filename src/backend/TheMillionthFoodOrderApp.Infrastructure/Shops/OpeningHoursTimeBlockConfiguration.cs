using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Infrastructure.Shops;

public sealed class OpeningHoursTimeBlockConfiguration : IEntityTypeConfiguration<OpeningHoursTimeBlock>
{
    public void Configure(EntityTypeBuilder<OpeningHoursTimeBlock> builder)
    {
        builder.ToTable("OpeningHoursTimeBlocks");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.ShopId)
            .IsRequired();

        builder.Property(b => b.DayOfWeek)
            .IsRequired()
            .HasConversion<int>(); // stored as 0-6 (System.DayOfWeek values)

        builder.Property(b => b.OpenTime)
            .IsRequired()
            .HasColumnType("time(7)");

        builder.Property(b => b.CloseTime)
            .IsRequired()
            .HasColumnType("time(7)");

        // Efficient lookup: all blocks for a specific shop-day combination
        builder.HasIndex(b => new { b.ShopId, b.DayOfWeek })
            .HasDatabaseName("IX_OpeningHoursTimeBlocks_ShopId_DayOfWeek");

        // Domain events are transient — never persisted
        builder.Ignore(b => b.DomainEvents);
    }
}
