using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

public sealed class OpeningHoursTimeBlockTests
{
    private static readonly Guid ValidShopId = Guid.CreateVersion7();

    // ── Create (happy path) ───────────────────────────────────────────────────

    [Test]
    public async Task Create_WithValidTimes_ReturnsTimeBlock()
    {
        var block = OpeningHoursTimeBlock.Create(
            ValidShopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));

        await Assert.That(block).IsNotNull();
        await Assert.That(block.Id).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    public async Task Create_PersistsDayAndTimes()
    {
        var open = new TimeOnly(9, 0);
        var close = new TimeOnly(17, 0);

        var block = OpeningHoursTimeBlock.Create(ValidShopId, DayOfWeek.Wednesday, open, close);

        await Assert.That(block.DayOfWeek).IsEqualTo(DayOfWeek.Wednesday);
        await Assert.That(block.OpenTime).IsEqualTo(open);
        await Assert.That(block.CloseTime).IsEqualTo(close);
    }

    [Test]
    public async Task Create_PersistsShopId()
    {
        var block = OpeningHoursTimeBlock.Create(
            ValidShopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));

        await Assert.That(block.ShopId).IsEqualTo(ValidShopId);
    }

    [Test]
    public async Task Create_GeneratesUuidV7()
    {
        var block = OpeningHoursTimeBlock.Create(
            ValidShopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0));

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (block.Id.ToByteArray()[7] >> 4) & 0x0F;
        await Assert.That(version).IsEqualTo(7);
    }

    [Arguments(DayOfWeek.Sunday)]
    [Arguments(DayOfWeek.Monday)]
    [Arguments(DayOfWeek.Tuesday)]
    [Arguments(DayOfWeek.Wednesday)]
    [Arguments(DayOfWeek.Thursday)]
    [Arguments(DayOfWeek.Friday)]
    [Arguments(DayOfWeek.Saturday)]
    [Test]
    public async Task Create_WithEachDayOfWeek_PersistsDay(DayOfWeek day)
    {
        var block = OpeningHoursTimeBlock.Create(
            ValidShopId, day, new TimeOnly(9, 0), new TimeOnly(17, 0));

        await Assert.That(block.DayOfWeek).IsEqualTo(day);
    }

    // ── Create (zero-length block) ────────────────────────────────────────────

    [Test]
    public async Task Create_WhenCloseEqualsOpen_ThrowsArgumentException()
    {
        var time = new TimeOnly(9, 0);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(OpeningHoursTimeBlock.Create(ValidShopId, DayOfWeek.Monday, time, time)));
    }

    // ── Create (overnight block not supported) ────────────────────────────────

    [Test]
    public async Task Create_WhenCloseBeforeOpen_ThrowsArgumentException()
    {
        var open = new TimeOnly(18, 0);
        var close = new TimeOnly(9, 0); // Earlier than open — overnight not supported

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(OpeningHoursTimeBlock.Create(ValidShopId, DayOfWeek.Monday, open, close)));
    }

    [Test]
    public async Task Create_WhenCloseBeforeOpen_ExceptionNamedParameterIsClose()
    {
        var open = new TimeOnly(18, 0);
        var close = new TimeOnly(9, 0);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(OpeningHoursTimeBlock.Create(ValidShopId, DayOfWeek.Monday, open, close)));

        await Assert.That(ex!.ParamName).IsEqualTo("close");
    }
}
