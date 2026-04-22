# FastEndpoints

## Purpose
API endpoint framework — one class per endpoint, replaces controllers and MediatR.

## Patterns

### Endpoint Structure
One class per endpoint. Keep the request record, endpoint class, and validator in the same file, in this order: **request first, endpoint second, validator last** (or split into separate files for large validators).

```csharp
public sealed record UpdateThingRequest(
    [property: RouteParam] string Slug,
    string Name,
    string Currency);

public sealed class UpdateThingEndpoint(IThingService service)
    : Endpoint<UpdateThingRequest, ThingResponse>
{
    public const string Route = "/api/brands/{brandSlug}/things/{slug}";

    public override void Configure()
    {
        Put(Route);
        AllowAnonymous(); // or Roles("Admin"), Policies("BrandAccess"), etc.
        PreProcessor<BrandScopedPreProcessor<UpdateThingRequest>>(); // for brand-scoped routes
        Summary(s =>
        {
            s.Summary = "Update a thing";
            s.Description = "Updates the thing for the given slug.";
            s.Response<ThingResponse>(200, "Thing updated.");
            s.Response(400, "Validation error.");
            s.Response(404, "Thing not found.");
        });
    }

    public override async Task HandleAsync(UpdateThingRequest req, CancellationToken ct)
    {
        var result = await service.UpdateAsync(req, ct);
        if (result is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }
        await HttpContext.Response.SendAsync(result, statusCode: 200, cancellation: ct);
    }
}

public sealed class UpdateThingValidator : Validator<UpdateThingRequest>
{
    public UpdateThingValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Currency).Length(3); // ISO 4217
    }
}
```

### Pre-Processors (Short-Circuiting)
Pre-processors run before the endpoint handler. To **short-circuit** and prevent the handler from executing:

1. Use `context.ValidationFailures.Add(...)` + `context.HttpContext.Response.SendErrorsAsync()`
2. Always check `context.HttpContext.ResponseStarted()` first (guards against multiple pre-processors writing)
3. Return `Task.CompletedTask` (not `async Task`) when no async work is needed

```csharp
public sealed class MyPreProcessor<TRequest> : IPreProcessor<TRequest>
{
    public Task PreProcessAsync(IPreProcessorContext<TRequest> context, CancellationToken ct)
    {
        if (context.HttpContext.ResponseStarted())
            return Task.CompletedTask;

        // Check your condition
        if (conditionFails)
        {
            context.ValidationFailures.Add(
                new ValidationFailure("Field", "Error message."));
            return context.HttpContext.Response.SendErrorsAsync(context.ValidationFailures, cancellation: ct);
        }

        return Task.CompletedTask;
    }
}
```

**Wrong way** (handler still executes):
```csharp
// DON'T DO THIS — handler will still run after WriteAsJsonAsync
context.HttpContext.Response.StatusCode = 400;
await context.HttpContext.Response.WriteAsJsonAsync(new { error = "..." }, ct);
```

### Brand-Scoped Endpoints
All endpoints under `/api/brands/{brandSlug}/...` must add:
```csharp
PreProcessor<BrandScopedPreProcessor<TRequest>>();
```
This validates that `IBrandContextAccessor.BrandSlug` is set before the handler runs.

### FluentValidation Integration
Use `Validator<TRequest>` base class. FastEndpoints auto-discovers and runs validators before the handler.

```csharp
public sealed class UpdateThingValidator : Validator<UpdateThingRequest>
{
    public UpdateThingValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Currency).Length(3); // ISO 4217
    }
}
```

## Gotchas

- **Pre-processor short-circuit**: Using `HttpContext.Response.WriteAsJsonAsync()` does NOT prevent the handler from executing. You must use `SendErrorsAsync()`, `SendForbiddenAsync()`, or similar FastEndpoints response methods.
- **Route params**: Use `[property: RouteParam]` on record properties that bind from route segments.
- **Middleware ordering**: `BrandContextMiddleware` must run before `UseFastEndpoints()` but after `UseAuthentication()`/`UseAuthorization()`.
