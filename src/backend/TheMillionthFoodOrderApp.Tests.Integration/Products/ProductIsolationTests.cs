using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Products;

/// <summary>
/// Integration tests verifying cross-brand isolation for products.
/// Products created in Brand Alpha must not be visible to Brand Beta.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class ProductIsolationTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    [Test]
    public async Task CreateProductInAlpha_NotVisibleInGamma()
    {
        var client = CreateClient();

        // Create product in Alpha
        var request = new
        {
            BasePrice = 5.00m,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Alpha-Only Product", Description = (string?)null },
            }
        };
        var createResponse = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.AlphaSlug), request);
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // List products in Gamma (empty brand) — should not contain Alpha's product
        var gammaList = await client.GetAsync(ProductsUrl(IntegrationTestBase.GammaSlug));
        await Assert.That(gammaList.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var gammaProducts = await gammaList.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(gammaProducts).IsNotNull();
        await Assert.That(gammaProducts!.Any(p => p.Id == created!.Id)).IsFalse();
    }

    [Test]
    public async Task BothBrandsHaveIndependentProducts()
    {
        var client = CreateClient();

        // Create different products in Alpha and Beta
        var alphaRequest = new
        {
            BasePrice = 3.00m,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Alpha Friet", Description = (string?)null },
            }
        };

        var betaRequest = new
        {
            BasePrice = 7.00m,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Beta Burger", Description = (string?)null },
            }
        };

        var alphaCreate = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.AlphaSlug), alphaRequest);
        await Assert.That(alphaCreate.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var alphaProduct = await alphaCreate.Content.ReadFromJsonAsync<ProductResponse>();

        var betaCreate = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.BetaSlug), betaRequest);
        await Assert.That(betaCreate.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var betaProduct = await betaCreate.Content.ReadFromJsonAsync<ProductResponse>();

        // Alpha's list should contain Alpha's product but not Beta's
        var alphaList = await client.GetAsync(ProductsUrl(IntegrationTestBase.AlphaSlug));
        var alphaProducts = await alphaList.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(alphaProducts!.Any(p => p.Id == alphaProduct!.Id)).IsTrue();
        await Assert.That(alphaProducts.Any(p => p.Id == betaProduct!.Id)).IsFalse();

        // Beta's list should contain Beta's product but not Alpha's
        var betaList = await client.GetAsync(ProductsUrl(IntegrationTestBase.BetaSlug));
        var betaProducts = await betaList.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(betaProducts!.Any(p => p.Id == betaProduct!.Id)).IsTrue();
        await Assert.That(betaProducts.Any(p => p.Id == alphaProduct!.Id)).IsFalse();
    }
}
