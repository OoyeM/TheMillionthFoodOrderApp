using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;
using Wolverine;
using Wolverine.Runtime;

namespace TheMillionthFoodOrderApp.Tests.Integration.DomainEvents;

[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class BrandEventDispatchTests(IntegrationTestBase fixture)
{
    private PlatformDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PlatformDbContext>()
            .UseSqlServer(fixture.PlatformConnectionString)
            .Options;
        return new PlatformDbContext(options);
    }

    [Test]
    public async Task SaveChangesAsync_publishes_BrandCreatedEvent_after_brand_is_added()
    {
        // Arrange
        var spy = new SpyMessageBus();
        await using var dbContext = CreateDbContext();
        var repository = new BrandRepository(dbContext, spy);

        var brand = Brand.Create("Dispatch Test Brand", "dispatch-test", "dispatch@test.com", null);
        await repository.AddAsync(brand);

        // Act
        await repository.SaveChangesAsync(CancellationToken.None);

        // Assert — one BrandCreatedEvent published
        await Assert.That(spy.Published.Count).IsEqualTo(1);
        await Assert.That(spy.Published[0]).IsTypeOf<BrandCreatedEvent>();

        var published = (BrandCreatedEvent)spy.Published[0];
        await Assert.That(published.Slug).IsEqualTo("dispatch-test");
    }

    [Test]
    public async Task SaveChangesAsync_does_not_publish_events_when_no_events_raised()
    {
        // Arrange — load an existing brand and read it (no mutation = no events)
        var spy = new SpyMessageBus();
        await using var dbContext = CreateDbContext();
        var repository = new BrandRepository(dbContext, spy);

        // The alpha brand was seeded by IntegrationTestBase without going through the repository,
        // so no pending events exist on the tracked entity.
        var existing = await dbContext.Brands.FirstAsync(b => b.Slug == IntegrationTestBase.AlphaSlug);

        // Act — save with no domain events raised
        await repository.SaveChangesAsync(CancellationToken.None);

        // Assert
        await Assert.That(spy.Published.Count).IsEqualTo(0);
    }
}

internal sealed class SpyMessageBus : IMessageBus
{
    public List<object> Published { get; } = [];

    public string? TenantId { get; set; }

    ValueTask IMessageBus.PublishAsync<T>(T message, DeliveryOptions? options)
    {
        Published.Add(message);
        return ValueTask.CompletedTask;
    }

    ValueTask IMessageBus.SendAsync<T>(T message, DeliveryOptions? options)
        => throw new NotImplementedException();

    ValueTask IMessageBus.BroadcastToTopicAsync(string topicName, object message, DeliveryOptions? options)
        => throw new NotImplementedException();

    Task ICommandBus.InvokeAsync(object message, CancellationToken cancellation, TimeSpan? timeout)
        => throw new NotImplementedException();

    Task<T> ICommandBus.InvokeAsync<T>(object message, CancellationToken cancellation, TimeSpan? timeout)
        => throw new NotImplementedException();

    Task ICommandBus.InvokeAsync(object message, DeliveryOptions options, CancellationToken cancellation, TimeSpan? timeout)
        => throw new NotImplementedException();

    Task<T> ICommandBus.InvokeAsync<T>(object message, DeliveryOptions options, CancellationToken cancellation, TimeSpan? timeout)
        => throw new NotImplementedException();

    Task IMessageBus.InvokeForTenantAsync(string tenantId, object message, CancellationToken cancellation, TimeSpan? timeout)
        => throw new NotImplementedException();

    Task<T> IMessageBus.InvokeForTenantAsync<T>(string tenantId, object message, CancellationToken cancellation, TimeSpan? timeout)
        => throw new NotImplementedException();

    IDestinationEndpoint IMessageBus.EndpointFor(string endpointName)
        => throw new NotImplementedException();

    IDestinationEndpoint IMessageBus.EndpointFor(Uri uri)
        => throw new NotImplementedException();

    IReadOnlyList<Envelope> IMessageBus.PreviewSubscriptions(object message)
        => throw new NotImplementedException();

    IReadOnlyList<Envelope> IMessageBus.PreviewSubscriptions(object message, DeliveryOptions options)
        => throw new NotImplementedException();
}
