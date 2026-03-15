using FastEndpoints;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Multitenancy;

namespace TheMillionthFoodOrderApp.Api.Endpoints;

/// <summary>
/// FastEndpoints pre-processor that verifies a brand context is active before the endpoint
/// handler runs. Add to any brand-scoped endpoint via:
/// <code>
///   PreProcessor&lt;BrandScopedPreProcessor&lt;TRequest&gt;&gt;()
/// </code>
///
/// Short-circuits with 400 Bad Request if no brand slug has been resolved — this should not
/// happen on correctly configured brand-scoped routes (the <c>BrandContextMiddleware</c> sets
/// the slug from the <c>{brandSlug}</c> route parameter), but guards against misconfiguration.
/// </summary>
public sealed class BrandScopedPreProcessor<TRequest> : IPreProcessor<TRequest>
{
    public Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        if (context.HttpContext.ResponseStarted())
            return Task.CompletedTask;

        var accessor = context.HttpContext.RequestServices.GetRequiredService<IBrandContextAccessor>();

        if (!string.IsNullOrWhiteSpace(accessor.BrandSlug))
            return Task.CompletedTask;

        context.ValidationFailures.Add(
            new ValidationFailure("BrandSlug", "No brand context is active for this request."));

        return context.HttpContext.Response.SendErrorsAsync(context.ValidationFailures, cancellation: ct);
    }
}
