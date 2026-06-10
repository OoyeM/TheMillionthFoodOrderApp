using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Infrastructure.Shops;

public sealed class ShopConfiguration : IEntityTypeConfiguration<Shop>
{
    public void Configure(EntityTypeBuilder<Shop> builder)
    {
        builder.ToTable("Shops");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(s => s.Slug)
            .IsRequired()
            .HasMaxLength(100);

        // Slug must be unique within the brand database — enforced at DB level
        builder.HasIndex(s => s.Slug)
            .IsUnique();

        builder.Property(s => s.ContactEmail)
            .IsRequired()
            .HasMaxLength(320); // RFC 5321 maximum

        builder.Property(s => s.ContactPhone)
            .HasMaxLength(30);

        // Optional VAT / enterprise number printed on customer receipts (US-FP-052).
        builder.Property(s => s.VatNumber)
            .HasMaxLength(30);

        builder.Property(s => s.IsActive)
            .IsRequired();

        builder.Property(s => s.TimeZoneId)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("Europe/Brussels");

        builder.Property(s => s.KitchenDisplayEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.TicketPrinterEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.PushNotificationEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(s => s.SoundAlertEnabled)
            .IsRequired()
            .HasDefaultValue(false);

        // Eat-in ordering config (US-FP-066) — owned, embedded in the Shops table.
        // NOTE: no HasDefaultValue on these bools. Replacing an owned value-object instance is
        // saved with Added-entity semantics, and EF omits columns whose value equals the database
        // default (the sentinel) — silently keeping the old row value on toggle. The defaults that
        // backfilled existing rows live in the AddShopEatInAndTimeSlotSettings migration history;
        // the domain always materialises these VOs, so inserts never rely on a column default.
        builder.OwnsOne(s => s.EatIn, eatIn =>
        {
            eatIn.Property(e => e.IsEnabled)
                .HasColumnName("EatIn_IsEnabled")
                .IsRequired();

            eatIn.Property(e => e.RequiresTableNumber)
                .HasColumnName("EatIn_RequiresTableNumber")
                .IsRequired();
        });
        builder.Navigation(s => s.EatIn).IsRequired();

        // Time-slot ordering config (US-FP-020) — owned; interval/max are null when disabled.
        builder.OwnsOne(s => s.TimeSlotOrdering, timeSlot =>
        {
            timeSlot.Property(t => t.IsEnabled)
                .HasColumnName("TimeSlotOrdering_IsEnabled")
                .IsRequired();

            timeSlot.Property(t => t.Interval)
                .HasColumnName("TimeSlotOrdering_Interval");

            timeSlot.Property(t => t.MaxOrdersPerInterval)
                .HasColumnName("TimeSlotOrdering_MaxOrdersPerInterval");
        });
        builder.Navigation(s => s.TimeSlotOrdering).IsRequired();

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        builder.Property(s => s.UpdatedAt)
            .IsRequired();

        // Address is an owned entity — columns are embedded in the Shops table
        builder.OwnsOne(s => s.Address, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("Address_Street")
                .IsRequired()
                .HasMaxLength(200);

            address.Property(a => a.Number)
                .HasColumnName("Address_Number")
                .IsRequired()
                .HasMaxLength(20);

            address.Property(a => a.City)
                .HasColumnName("Address_City")
                .IsRequired()
                .HasMaxLength(100);

            address.Property(a => a.PostalCode)
                .HasColumnName("Address_PostalCode")
                .IsRequired()
                .HasMaxLength(20);

            address.Property(a => a.Country)
                .HasColumnName("Address_Country")
                .IsRequired()
                .HasMaxLength(2); // ISO 3166-1 alpha-2
        });

        // Opening hours blocks — cascade delete so removing a shop removes its schedule
        builder.HasMany(s => s.OpeningHours)
            .WithOne()
            .HasForeignKey(b => b.ShopId)
            .OnDelete(DeleteBehavior.Cascade);

        // Domain events are transient — never persisted
        builder.Ignore(s => s.DomainEvents);
    }
}
