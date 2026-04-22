using Shouldly;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Tests.Unit.OrderLifecycle;

public sealed class OrderLifecycleConfigTests
{
    private static readonly Guid ShopId = Guid.NewGuid();

    // ── CreateDefault ─────────────────────────────────────────────────────────

    [Fact]
    public void CreateDefault_HasSixStatuses()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        config.Statuses.Count.ShouldBe(6);
    }

    [Fact]
    public void CreateDefault_SortOrdersAreSequentialZeroToFive()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        var sortOrders = config.Statuses.Select(s => s.SortOrder).OrderBy(o => o).ToList();
        for (var i = 0; i < sortOrders.Count; i++)
            sortOrders[i].ShouldBe(i);
    }

    [Fact]
    public void CreateDefault_HasExactlyTwoTerminalStatuses()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        config.Statuses.Count(s => s.IsTerminal).ShouldBe(2);
    }

    [Fact]
    public void CreateDefault_TerminalStatusesArePickedUpAndDelivered()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        var terminalKeys = config.Statuses
            .Where(s => s.IsTerminal)
            .Select(s => s.SystemKey)
            .ToList();

        terminalKeys.ShouldContain("picked_up");
        terminalKeys.ShouldContain("delivered");
    }

    [Fact]
    public void CreateDefault_HasFiveTransitions()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        config.Transitions.Count.ShouldBe(5);
    }

    [Fact]
    public void CreateDefault_AllTransitionsReferenceStatusesInConfig()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var statusIds = new HashSet<Guid>(config.Statuses.Select(s => s.Id));

        foreach (var transition in config.Transitions)
        {
            statusIds.ShouldContain(transition.FromStatusId);
            statusIds.ShouldContain(transition.ToStatusId);
        }
    }

    [Fact]
    public void CreateDefault_SetsShopId()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        config.ShopId.ShouldBe(ShopId);
    }

    // ── ConfigureLifecycle — happy path ───────────────────────────────────────

    [Fact]
    public void ConfigureLifecycle_WithValidCustomSet_ReplacesStatusesAtomically()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        var newStatus1 = OrderStatus.Create(config.Id, "Open", null, 0, false);
        var newStatus2 = OrderStatus.Create(config.Id, "Done", null, 1, true);
        var newTransition = OrderStatusTransition.Create(config.Id, newStatus1.Id, newStatus2.Id);

        config.ConfigureLifecycle([newStatus1, newStatus2], [newTransition]);

        config.Statuses.Count.ShouldBe(2);
        config.Transitions.Count.ShouldBe(1);
        config.Statuses.ShouldContain(s => s.Name == "Open");
        config.Statuses.ShouldContain(s => s.Name == "Done");
    }

    [Fact]
    public void ConfigureLifecycle_WithValidCustomSet_UpdatesUpdatedAt()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var originalUpdatedAt = config.UpdatedAt;

        var s1 = OrderStatus.Create(config.Id, "Start", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "End", null, 1, true);

        config.ConfigureLifecycle([s1, s2], []);

        config.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalUpdatedAt);
    }

    // ── ConfigureLifecycle — validation guards ────────────────────────────────

    [Fact]
    public void ConfigureLifecycle_WithFewerThanTwoStatuses_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var single = OrderStatus.Create(config.Id, "Only", null, 0, true);

        Should.Throw<ArgumentException>(() =>
            config.ConfigureLifecycle([single], []));
    }

    [Fact]
    public void ConfigureLifecycle_WithZeroTerminalStatuses_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var s1 = OrderStatus.Create(config.Id, "A", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "B", null, 1, false);

        Should.Throw<ArgumentException>(() =>
            config.ConfigureLifecycle([s1, s2], []));
    }

    [Fact]
    public void ConfigureLifecycle_WithNonSequentialSortOrders_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var s1 = OrderStatus.Create(config.Id, "A", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "B", null, 1, false);
        var s3 = OrderStatus.Create(config.Id, "C", null, 3, true); // gap: missing sort order 2

        Should.Throw<ArgumentException>(() =>
            config.ConfigureLifecycle([s1, s2, s3], []));
    }

    [Fact]
    public void ConfigureLifecycle_TransitionWithUnknownFromStatusId_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var s1 = OrderStatus.Create(config.Id, "A", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "B", null, 1, true);
        var unknownId = Guid.NewGuid();
        var badTransition = OrderStatusTransition.Create(config.Id, unknownId, s2.Id);

        Should.Throw<ArgumentException>(() =>
            config.ConfigureLifecycle([s1, s2], [badTransition]));
    }

    [Fact]
    public void ConfigureLifecycle_TransitionWithUnknownToStatusId_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var s1 = OrderStatus.Create(config.Id, "A", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "B", null, 1, true);
        var unknownId = Guid.NewGuid();
        var badTransition = OrderStatusTransition.Create(config.Id, s1.Id, unknownId);

        Should.Throw<ArgumentException>(() =>
            config.ConfigureLifecycle([s1, s2], [badTransition]));
    }
}
