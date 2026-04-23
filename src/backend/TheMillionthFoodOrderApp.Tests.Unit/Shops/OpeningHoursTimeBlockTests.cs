using Shouldly;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

public sealed class OpeningHoursTimeBlockTests
{
    private static readonly Guid ValidShopId = Guid.CreateVersion7();

    // ── Create (happy path) ───────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidTimes_ReturnsTimeBlock()
    {
        var block = OpeningHoursTimeBlock.Create(
            ValidShopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));

        block.ShouldNotBeNull();
        block.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Create_PersistsDayAndTimes()
    {
        var open = new TimeOnly(9, 0);
        var close = new TimeOnly(17, 0);

        var block = OpeningHoursTimeBlock.Create(ValidShopId, DayOfWeek.Wednesday, open, close);

        block.DayOfWeek.ShouldBe(DayOfWeek.Wednesday);
        block.OpenTime.ShouldBe(open);
        block.CloseTime.ShouldBe(close);
    }

    [Fact]
    public void Create_PersistsShopId()
    {
        var block = OpeningHoursTimeBlock.Create(
            ValidShopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));

        block.ShopId.ShouldBe(ValidShopId);
    }

    [Fact]
    public void Create_GeneratesUuidV7()
    {
        var block = OpeningHoursTimeBlock.Create(
            ValidShopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (block.Id.ToByteArray()[7] >> 4) & 0x0F;
        version.ShouldBe(7);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    public void Create_WithEachDayOfWeek_PersistsDay(DayOfWeek day)
    {
        var block = OpeningHoursTimeBlock.Create(
            ValidShopId, day, new TimeOnly(9, 0), new TimeOnly(17, 0));

        block.DayOfWeek.ShouldBe(day);
    }

    // ── Create (zero-length block) ────────────────────────────────────────────

    [Fact]
    public void Create_WhenCloseEqualsOpen_ThrowsArgumentException()
    {
        var time = new TimeOnly(9, 0);

        Should.Throw<ArgumentException>(() =>
            OpeningHoursTimeBlock.Create(ValidShopId, DayOfWeek.Monday, time, time));
    }

    // ── Create (overnight block not supported) ────────────────────────────────

    [Fact]
    public void Create_WhenCloseBeforeOpen_ThrowsArgumentException()
    {
        var open = new TimeOnly(18, 0);
        var close = new TimeOnly(9, 0); // Earlier than open — overnight not supported

        Should.Throw<ArgumentException>(() =>
            OpeningHoursTimeBlock.Create(ValidShopId, DayOfWeek.Monday, open, close));
    }

    [Fact]
    public void Create_WhenCloseBeforeOpen_ExceptionNamedParameterIsClose()
    {
        var open = new TimeOnly(18, 0);
        var close = new TimeOnly(9, 0);

        var ex = Should.Throw<ArgumentException>(() =>
            OpeningHoursTimeBlock.Create(ValidShopId, DayOfWeek.Monday, open, close));

        ex.ParamName.ShouldBe("close");
    }
}
