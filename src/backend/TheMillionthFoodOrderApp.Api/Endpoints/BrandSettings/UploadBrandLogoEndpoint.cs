using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.BrandSettings;

namespace TheMillionthFoodOrderApp.Api.Endpoints.BrandSettings;

public sealed class UploadBrandLogoRequest
{
    /// <summary>Brand slug from the route parameter.</summary>
    [RouteParam]
    public string BrandSlug { get; init; } = string.Empty;

    /// <summary>The uploaded logo file. Bound from the multipart form data field named "logo".</summary>
    public IFormFile? Logo { get; init; }
}

public sealed class UploadBrandLogoRequestValidator : Validator<UploadBrandLogoRequest>
{
    private static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/svg+xml",
    ];

    private const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

    public UploadBrandLogoRequestValidator()
    {
        RuleFor(x => x.Logo)
            .NotNull().WithMessage("A logo file is required.");

        When(x => x.Logo is not null, () =>
        {
            RuleFor(x => x.Logo!.Length)
                .LessThanOrEqualTo(MaxFileSizeBytes)
                .WithMessage($"Logo file must not exceed {MaxFileSizeBytes / 1024 / 1024} MB.");

            RuleFor(x => x.Logo!.ContentType)
                .Must(ct => AllowedContentTypes.Contains(ct, StringComparer.OrdinalIgnoreCase))
                .WithMessage($"Logo must be one of: {string.Join(", ", AllowedContentTypes)}.");
        });
    }
}

public sealed class UploadBrandLogoEndpoint(IBrandSettingsService brandSettingsService)
    : Endpoint<UploadBrandLogoRequest, UploadBrandLogoResponse>
{
    public const string Route = "/api/brands/{brandSlug}/settings/logo";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
        AllowFileUploads();
        PreProcessor<BrandScopedPreProcessor<UploadBrandLogoRequest>>();
        Summary(s =>
        {
            s.Summary = "Upload brand logo";
            s.Description = "Uploads a new logo image for the brand. Replaces the previous logo if one exists. " +
                            "Send as multipart/form-data with a 'logo' field containing the image file.";
            s.Response<UploadBrandLogoResponse>(200, "Logo uploaded successfully. Returns the public URL.");
            s.Response(400, "Validation error (file too large, wrong type, etc.).");
            s.Response(404, "Brand not found or settings not yet provisioned.");
        });
    }

    public override async Task HandleAsync(UploadBrandLogoRequest req, CancellationToken ct)
    {
        var logo = req.Logo!;

        await using var stream = logo.OpenReadStream();

        var response = await brandSettingsService.UploadLogoAsync(
            logo.FileName,
            logo.ContentType,
            stream,
            ct);

        if (response is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}
