using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record SetOpeningHoursRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id,
    List<TimeBlockRequest> TimeBlocks);

public sealed record TimeBlockRequest(DayOfWeek DayOfWeek, string OpenTime, string CloseTime);

public sealed class SetOpeningHoursRequestValidator : Validator<SetOpeningHoursRequest>
{
    private static readonly System.Text.RegularExpressions.Regex TimePattern =
        new(@"^([01]\d|2[0-3]):([0-5]\d)$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public SetOpeningHoursRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Shop id is required.");

        RuleFor(x => x.TimeBlocks)
            .NotNull().WithMessage("TimeBlocks must not be null.");

        RuleForEach(x => x.TimeBlocks).ChildRules(block =>
        {
            block.RuleFor(b => b.DayOfWeek)
                .IsInEnum().WithMessage("DayOfWeek must be a value between 0 (Sunday) and 6 (Saturday).");

            block.RuleFor(b => b.OpenTime)
                .NotEmpty().WithMessage("OpenTime is required.")
                .Matches(TimePattern).WithMessage("OpenTime must be in HH:mm format (e.g. '09:00').");

            block.RuleFor(b => b.CloseTime)
                .NotEmpty().WithMessage("CloseTime is required.")
                .Matches(TimePattern).WithMessage("CloseTime must be in HH:mm format (e.g. '17:00').");

            block.RuleFor(b => b)
                .Must(b =>
                {
                    if (!TimeOnly.TryParse(b.OpenTime, out var open) ||
                        !TimeOnly.TryParse(b.CloseTime, out var close))
                        return true; // format errors reported above
                    return close > open;
                })
                .WithMessage("CloseTime must be after OpenTime. Overnight blocks (e.g. 22:00-02:00) are not supported.")
                .WithName("TimeBlock");
        });

        // Validate no duplicate/overlapping blocks per day (cross-block rule)
        RuleFor(x => x.TimeBlocks)
            .Must(blocks =>
            {
                if (blocks is null) return true;
                var byDay = blocks
                    .Where(b => TimeOnly.TryParse(b.OpenTime, out _) && TimeOnly.TryParse(b.CloseTime, out _))
                    .GroupBy(b => b.DayOfWeek);

                foreach (var group in byDay)
                {
                    var sorted = group
                        .Select(b => (Open: TimeOnly.Parse(b.OpenTime), Close: TimeOnly.Parse(b.CloseTime)))
                        .OrderBy(t => t.Open)
                        .ToList();

                    for (var i = 0; i < sorted.Count - 1; i++)
                    {
                        if (sorted[i].Close > sorted[i + 1].Open)
                            return false;
                    }
                }

                return true;
            })
            .WithMessage("Time blocks on the same day must not overlap.");
    }
}

public sealed class SetOpeningHoursEndpoint(IOpeningHoursService openingHoursService)
    : Endpoint<SetOpeningHoursRequest, OpeningHoursResponse>
{
    public override void Configure()
    {
        Put("/api/brands/{brandSlug}/shops/{id}/opening-hours");
        // TODO: Require ShopManager role when auth is implemented (US-FP-039)
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<SetOpeningHoursRequest>>();
        Summary(s =>
        {
            s.Summary = "Set opening hours for a shop";
            s.Description = "Shop Manager replaces the complete weekly opening hours schedule. Existing blocks are cleared and replaced atomically.";
            s.Response<OpeningHoursResponse>(200, "Opening hours updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(SetOpeningHoursRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new Application.Shops.SetOpeningHoursRequest(
                req.TimeBlocks
                    .Select(b => new Application.Shops.TimeBlockRequest(b.DayOfWeek, b.OpenTime, b.CloseTime))
                    .ToList());

            var response = await openingHoursService.SetOpeningHoursAsync(req.Id, appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
        catch (ArgumentException ex)
        {
            var failures = new List<ValidationFailure>
            {
                new("timeBlocks", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}
