using TheMillionthFoodOrderApp.Domain.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Tests.Unit.OrderLifecycle;

public sealed class OrderLifecycleConfigTests
{
    private static readonly Guid ShopId = Guid.NewGuid();

    // ── CreateDefault ─────────────────────────────────────────────────────────

    [Test]
    public async Task CreateDefault_HasSixStatuses()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        await Assert.That(config.Statuses.Count).IsEqualTo(6);
    }

    [Test]
    public async Task CreateDefault_SortOrdersAreSequentialZeroToFive()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        var sortOrders = config.Statuses.Select(s => s.SortOrder).OrderBy(o => o).ToList();
        for (var i = 0; i < sortOrders.Count; i++)
            await Assert.That(sortOrders[i]).IsEqualTo(i);
    }

    [Test]
    public async Task CreateDefault_HasExactlyTwoTerminalStatuses()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        await Assert.That(config.Statuses.Count(s => s.IsTerminal)).IsEqualTo(2);
    }

    [Test]
    public async Task CreateDefault_TerminalStatusesArePickedUpAndDelivered()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        var terminalKeys = config.Statuses
            .Where(s => s.IsTerminal)
            .Select(s => s.SystemKey)
            .ToList();

        await Assert.That(terminalKeys).Contains("picked_up");
        await Assert.That(terminalKeys).Contains("delivered");
    }

    [Test]
    public async Task CreateDefault_HasFiveTransitions()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        await Assert.That(config.Transitions.Count).IsEqualTo(5);
    }

    [Test]
    public async Task CreateDefault_AllTransitionsReferenceStatusesInConfig()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var statusIds = new HashSet<Guid>(config.Statuses.Select(s => s.Id));

        foreach (var transition in config.Transitions)
        {
            await Assert.That(statusIds).Contains(transition.FromStatusId);
            await Assert.That(statusIds).Contains(transition.ToStatusId);
        }
    }

    [Test]
    public async Task CreateDefault_SetsShopId()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        await Assert.That(config.ShopId).IsEqualTo(ShopId);
    }

    // ── ConfigureLifecycle — happy path ───────────────────────────────────────

    [Test]
    public async Task ConfigureLifecycle_WithValidCustomSet_ReplacesStatusesAtomically()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);

        var newStatus1 = OrderStatus.Create(config.Id, "Open", null, 0, false);
        var newStatus2 = OrderStatus.Create(config.Id, "Done", null, 1, true);
        var newTransition = OrderStatusTransition.Create(config.Id, newStatus1.Id, newStatus2.Id);

        config.ConfigureLifecycle([newStatus1, newStatus2], [newTransition]);

        await Assert.That(config.Statuses.Count).IsEqualTo(2);
        await Assert.That(config.Transitions.Count).IsEqualTo(1);
        await Assert.That(config.Statuses).Contains(s => s.Name == "Open");
        await Assert.That(config.Statuses).Contains(s => s.Name == "Done");
    }

    [Test]
    public async Task ConfigureLifecycle_WithValidCustomSet_UpdatesUpdatedAt()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var originalUpdatedAt = config.UpdatedAt;

        var s1 = OrderStatus.Create(config.Id, "Start", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "End", null, 1, true);

        config.ConfigureLifecycle([s1, s2], []);

        await Assert.That(config.UpdatedAt).IsGreaterThanOrEqualTo(originalUpdatedAt);
    }

    // ── ConfigureLifecycle — validation guards ────────────────────────────────

    [Test]
    public async Task ConfigureLifecycle_WithFewerThanTwoStatuses_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var single = OrderStatus.Create(config.Id, "Only", null, 0, true);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            config.ConfigureLifecycle([single], []);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task ConfigureLifecycle_WithZeroTerminalStatuses_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var s1 = OrderStatus.Create(config.Id, "A", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "B", null, 1, false);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            config.ConfigureLifecycle([s1, s2], []);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task ConfigureLifecycle_WithNonSequentialSortOrders_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var s1 = OrderStatus.Create(config.Id, "A", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "B", null, 1, false);
        var s3 = OrderStatus.Create(config.Id, "C", null, 3, true); // gap: missing sort order 2

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            config.ConfigureLifecycle([s1, s2, s3], []);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task ConfigureLifecycle_TransitionWithUnknownFromStatusId_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var s1 = OrderStatus.Create(config.Id, "A", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "B", null, 1, true);
        var unknownId = Guid.NewGuid();
        var badTransition = OrderStatusTransition.Create(config.Id, unknownId, s2.Id);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            config.ConfigureLifecycle([s1, s2], [badTransition]);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task ConfigureLifecycle_TransitionWithUnknownToStatusId_ThrowsArgumentException()
    {
        var config = OrderLifecycleConfig.CreateDefault(ShopId);
        var s1 = OrderStatus.Create(config.Id, "A", null, 0, false);
        var s2 = OrderStatus.Create(config.Id, "B", null, 1, true);
        var unknownId = Guid.NewGuid();
        var badTransition = OrderStatusTransition.Create(config.Id, s1.Id, unknownId);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            config.ConfigureLifecycle([s1, s2], [badTransition]);
            return Task.CompletedTask;
        });
    }
}
