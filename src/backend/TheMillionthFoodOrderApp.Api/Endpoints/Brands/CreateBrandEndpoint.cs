using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Brands;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Brands;

public sealed record CreateBrandRequest(
    string Name,
    string Slug,
    string ContactEmail,
    string? ContactPhone);

public sealed class CreateBrandRequestValidator : Validator<CreateBrandRequest>
{
    public CreateBrandRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(100)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be URL-safe: lowercase letters, digits, and hyphens only (no leading/trailing hyphens).");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("Contact email must be a valid email address.")
            .MaximumLength(320);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(30)
            .When(x => x.ContactPhone is not null);
    }
}

public sealed class CreateBrandEndpoint(IBrandService brandService)
    : Endpoint<CreateBrandRequest, BrandResponse>
{
    public const string Route = "/api/brands";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new brand";
            s.Description = "Platform Admin creates a brand. A dedicated database name is reserved for future provisioning.";
            s.Response<BrandResponse>(201, "Brand created successfully.");
            s.Response(400, "Validation error.");
            s.Response(409, "A brand with the same slug already exists.");
        });
    }

    public override async Task HandleAsync(CreateBrandRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new Application.Brands.CreateBrandRequest(
                req.Name,
                req.Slug,
                req.ContactEmail,
                req.ContactPhone);

            var response = await brandService.CreateBrandAsync(appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure>
            {
                new("slug", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 409, cancellation: ct);
        }
    }
}
