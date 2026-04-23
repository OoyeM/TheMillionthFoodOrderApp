using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Products;

/// <summary>
/// Integration tests for product CRUD operations.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class ProductCrudTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static string ProductUrl(string brandSlug, Guid id) =>
        $"/api/brands/{brandSlug}/products/{id}";

    private static object MakeCreateRequest(
        decimal basePrice = 3.50m,
        string? imageUrl = null,
        string nlName = "Test Product",
        string? frName = null) =>
        new
        {
            BasePrice = basePrice,
            ImageUrl = imageUrl,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = nlName, Description = (string?)"Test beschrijving" },
            }
            .Concat(frName is not null
                ? [new { LanguageCode = "fr", Name = frName, Description = (string?)"Description test" }]
                : [])
            .ToArray()
        };

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task CreateProduct_Returns201_WithTranslations()
    {
        var client = CreateClient();
        var request = MakeCreateRequest(nlName: "Frietje Speciaal", frName: "Frites Spécial");

        var response = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.AlphaSlug), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(product).IsNotNull();
        await Assert.That(product!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(product.BasePrice.Amount).IsEqualTo(3.50m);
        await Assert.That(product.BasePrice.Currency).IsEqualTo("EUR");
        await Assert.That(product.Translations.Count).IsEqualTo(2);
        await Assert.That(product.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Frietje Speciaal")).IsTrue();
        await Assert.That(product.Translations.Any(t => t.LanguageCode == "fr" && t.Name == "Frites Spécial")).IsTrue();
    }

    [Test]
    public async Task CreateProduct_WithoutTranslations_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            BasePrice = 3.50m,
            Translations = Array.Empty<object>()
        };

        var response = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.AlphaSlug), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateProduct_WithNegativePrice_Returns400()
    {
        var client = CreateClient();
        var request = MakeCreateRequest(basePrice: -1.00m);

        var response = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.AlphaSlug), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateProduct_WithZeroPrice_Returns400()
    {
        var client = CreateClient();
        var request = MakeCreateRequest(basePrice: 0m);

        var response = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.AlphaSlug), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateProduct_WithInvalidLanguageCode_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            BasePrice = 3.50m,
            Translations = new[] { new { LanguageCode = "en", Name = "Test", Description = (string?)null } }
        };

        var response = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.AlphaSlug), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task CreateProduct_WithDuplicateLanguageCodes_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            BasePrice = 3.50m,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Name 1", Description = (string?)null },
                new { LanguageCode = "nl", Name = "Name 2", Description = (string?)null },
            }
        };

        var response = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.AlphaSlug), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetProduct_Returns200_WithAllTranslations()
    {
        var client = CreateClient();

        // Create first
        var createResponse = await client.PostAsJsonAsync(
            ProductsUrl(IntegrationTestBase.AlphaSlug),
            MakeCreateRequest(nlName: "Bitterballen", frName: "Bitterballen"));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Get by id
        var getResponse = await client.GetAsync(
            ProductUrl(IntegrationTestBase.AlphaSlug, created!.Id));

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var product = await getResponse.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(product).IsNotNull();
        await Assert.That(product!.Id).IsEqualTo(created.Id);
        await Assert.That(product.Translations.Count).IsEqualTo(2);
    }

    [Test]
    public async Task GetProduct_NonExistent_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            ProductUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task ListProducts_ReturnsAll_ExcludesSoftDeleted()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        // Create two products
        var create1 = await client.PostAsJsonAsync(
            ProductsUrl(brand), MakeCreateRequest(nlName: "Product A"));
        var product1 = await create1.Content.ReadFromJsonAsync<ProductResponse>();

        var create2 = await client.PostAsJsonAsync(
            ProductsUrl(brand), MakeCreateRequest(nlName: "Product B"));
        var product2 = await create2.Content.ReadFromJsonAsync<ProductResponse>();

        // Soft-delete product1
        await client.DeleteAsync(ProductUrl(brand, product1!.Id));

        // List should contain product2 but not product1
        var listResponse = await client.GetAsync(ProductsUrl(brand));
        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var products = await listResponse.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(products).IsNotNull();
        await Assert.That(products!.Any(p => p.Id == product2!.Id)).IsTrue();
        await Assert.That(products.Any(p => p.Id == product1.Id)).IsFalse();
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateProduct_Returns200_WithUpdatedData()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create
        var createResponse = await client.PostAsJsonAsync(
            ProductsUrl(brand), MakeCreateRequest(basePrice: 3.50m, nlName: "Original Name"));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Update with new price and translations
        var updateRequest = new
        {
            BasePrice = 4.50m,
            ImageUrl = (string?)null,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Updated Name", Description = (string?)"Updated desc" },
                new { LanguageCode = "fr", Name = "Nom Mis à Jour", Description = (string?)null },
            }
        };

        var updateResponse = await client.PutAsJsonAsync(
            ProductUrl(brand, created!.Id), updateRequest);

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.BasePrice.Amount).IsEqualTo(4.50m);
        await Assert.That(updated.Translations.Count).IsEqualTo(2);
        await Assert.That(updated.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Updated Name")).IsTrue();
        await Assert.That(updated.Translations.Any(t => t.LanguageCode == "fr" && t.Name == "Nom Mis à Jour")).IsTrue();
    }

    [Test]
    public async Task UpdateProduct_NonExistent_Returns404()
    {
        var client = CreateClient();
        var updateRequest = new
        {
            BasePrice = 4.50m,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Test", Description = (string?)null },
            }
        };

        var response = await client.PutAsJsonAsync(
            ProductUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()), updateRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UpdateProduct_ShrinkTranslations_RemovesOldLanguage()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create with NL + FR
        var createRequest = new
        {
            BasePrice = 3.00m,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Twee Talen", Description = (string?)null },
                new { LanguageCode = "fr", Name = "Deux Langues", Description = (string?)null },
            }
        };
        var createResponse = await client.PostAsJsonAsync(ProductsUrl(brand), createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(created!.Translations.Count).IsEqualTo(2);

        // Update to NL only — FR should be removed
        var updateRequest = new
        {
            BasePrice = 3.00m,
            ImageUrl = (string?)null,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Eén Taal", Description = (string?)null },
            }
        };
        var updateResponse = await client.PutAsJsonAsync(
            ProductUrl(brand, created.Id), updateRequest);

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(updated!.Translations.Count).IsEqualTo(1);
        await Assert.That(updated.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Eén Taal")).IsTrue();
        await Assert.That(updated.Translations.Any(t => t.LanguageCode == "fr")).IsFalse();
    }

    [Test]
    public async Task UpdateProduct_WithDuplicateLanguageCodes_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a product first
        var createResponse = await client.PostAsJsonAsync(
            ProductsUrl(brand), MakeCreateRequest(nlName: "Dup Update Test"));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Try to update with duplicate language codes
        var updateRequest = new
        {
            BasePrice = 3.50m,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Name 1", Description = (string?)null },
                new { LanguageCode = "nl", Name = "Name 2", Description = (string?)null },
            }
        };
        var response = await client.PutAsJsonAsync(
            ProductUrl(brand, created!.Id), updateRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── Delete (soft-delete) ──────────────────────────────────────────────────

    [Test]
    public async Task DeleteProduct_Returns204_SoftDeletes()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create
        var createResponse = await client.PostAsJsonAsync(
            ProductsUrl(brand), MakeCreateRequest(nlName: "To Be Deleted"));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Delete
        var deleteResponse = await client.DeleteAsync(ProductUrl(brand, created!.Id));
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Get by id should return 404 (soft-deleted, filtered by global query filter)
        var getResponse = await client.GetAsync(ProductUrl(brand, created.Id));
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteProduct_NonExistent_Returns404()
    {
        var client = CreateClient();

        var response = await client.DeleteAsync(
            ProductUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteProduct_AlreadyDeleted_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create and delete
        var createResponse = await client.PostAsJsonAsync(
            ProductsUrl(brand), MakeCreateRequest(nlName: "Double Delete"));
        var created = await createResponse.Content.ReadFromJsonAsync<ProductResponse>();
        await client.DeleteAsync(ProductUrl(brand, created!.Id));

        // Second delete should 404 (already filtered out)
        var secondDelete = await client.DeleteAsync(ProductUrl(brand, created.Id));
        await Assert.That(secondDelete.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
