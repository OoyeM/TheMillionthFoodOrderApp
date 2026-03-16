using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Application.MenuCategories;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.MenuCategories;

/// <summary>
/// Integration tests for menu category CRUD operations.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
public sealed class MenuCategoryCrudTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string CategoriesUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/menu-categories";

    private static string CategoryUrl(string brandSlug, Guid id) =>
        $"/api/brands/{brandSlug}/menu-categories/{id}";

    private static string ReorderUrl(string brandSlug, Guid id) =>
        $"/api/brands/{brandSlug}/menu-categories/{id}/sort-order";

    private static string AssignProductUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/menu-categories/assign-product";

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static object MakeCreateRequest(
        string nlName = "Test Category",
        string? frName = null,
        int sortOrder = 0,
        string? imageUrl = null) =>
        new
        {
            ImageUrl = imageUrl,
            SortOrder = sortOrder,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = nlName, Description = (string?)"Test beschrijving" },
            }
            .Concat(frName is not null
                ? [new { LanguageCode = "fr", Name = frName, Description = (string?)"Description test" }]
                : [])
            .ToArray()
        };

    private static object MakeCreateProductRequest(string nlName = "Test Product") =>
        new
        {
            BasePrice = 3.50m,
            ImageUrl = (string?)null,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = nlName, Description = (string?)"Test beschrijving" },
            }
        };

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCategory_Returns201_WithTranslations()
    {
        var client = CreateClient();
        var request = MakeCreateRequest(nlName: "Starters", frName: "Entrées", sortOrder: 1);

        var response = await client.PostAsJsonAsync(
            CategoriesUrl(IntegrationTestBase.AlphaSlug), request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var category = await response.Content.ReadFromJsonAsync<MenuCategoryResponse>();
        category.ShouldNotBeNull();
        category.Id.ShouldNotBe(Guid.Empty);
        category.SortOrder.ShouldBe(1);
        category.Translations.Count.ShouldBe(2);
        category.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Starters");
        category.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Entrées");
    }

    [Fact]
    public async Task CreateCategory_WithoutTranslations_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            SortOrder = 0,
            Translations = Array.Empty<object>()
        };

        var response = await client.PostAsJsonAsync(
            CategoriesUrl(IntegrationTestBase.AlphaSlug), request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_WithNegativeSortOrder_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            SortOrder = -1,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Test", Description = (string?)null },
            }
        };

        var response = await client.PostAsJsonAsync(
            CategoriesUrl(IntegrationTestBase.AlphaSlug), request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_WithInvalidLanguageCode_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            SortOrder = 0,
            Translations = new[] { new { LanguageCode = "en", Name = "Test", Description = (string?)null } }
        };

        var response = await client.PostAsJsonAsync(
            CategoriesUrl(IntegrationTestBase.AlphaSlug), request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCategory_WithDuplicateLanguageCodes_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            SortOrder = 0,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Name 1", Description = (string?)null },
                new { LanguageCode = "nl", Name = "Name 2", Description = (string?)null },
            }
        };

        var response = await client.PostAsJsonAsync(
            CategoriesUrl(IntegrationTestBase.AlphaSlug), request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Get ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCategory_Returns200_WithAllData()
    {
        var client = CreateClient();

        // Create first
        var createResponse = await client.PostAsJsonAsync(
            CategoriesUrl(IntegrationTestBase.AlphaSlug),
            MakeCreateRequest(nlName: "Hoofdgerechten", frName: "Plats principaux", sortOrder: 2));
        var created = await createResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Get by id
        var getResponse = await client.GetAsync(
            CategoryUrl(IntegrationTestBase.AlphaSlug, created!.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var category = await getResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();
        category.ShouldNotBeNull();
        category.Id.ShouldBe(created.Id);
        category.SortOrder.ShouldBe(2);
        category.Translations.Count.ShouldBe(2);
        category.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Hoofdgerechten");
        category.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Plats principaux");
    }

    [Fact]
    public async Task GetCategory_NonExistent_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            CategoryUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListCategories_ReturnsOrderedBySortOrder()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        // Create three categories with different sort orders (out of order intentionally)
        var createC = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Category C", sortOrder: 30));
        var categoryC = await createC.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        var createA = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Category A", sortOrder: 10));
        var categoryA = await createA.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        var createB = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Category B", sortOrder: 20));
        var categoryB = await createB.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // List should be ordered by SortOrder ascending
        var listResponse = await client.GetAsync(CategoriesUrl(brand));
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var categories = await listResponse.Content.ReadFromJsonAsync<List<MenuCategoryListItemResponse>>();
        categories.ShouldNotBeNull();

        // Verify all three are present
        categories.ShouldContain(c => c.Id == categoryA!.Id);
        categories.ShouldContain(c => c.Id == categoryB!.Id);
        categories.ShouldContain(c => c.Id == categoryC!.Id);

        // Find their positions in the returned list and verify sort order
        var indexA = categories.FindIndex(c => c.Id == categoryA!.Id);
        var indexB = categories.FindIndex(c => c.Id == categoryB!.Id);
        var indexC = categories.FindIndex(c => c.Id == categoryC!.Id);
        indexA.ShouldBeLessThan(indexB);
        indexB.ShouldBeLessThan(indexC);
    }

    [Fact]
    public async Task ListCategories_ExcludesSoftDeleted()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        // Create two categories
        var create1 = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Category Keep", sortOrder: 1));
        var category1 = await create1.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        var create2 = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Category Delete", sortOrder: 2));
        var category2 = await create2.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Soft-delete category2
        await client.DeleteAsync(CategoryUrl(brand, category2!.Id));

        // List should contain category1 but not category2
        var listResponse = await client.GetAsync(CategoriesUrl(brand));
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var categories = await listResponse.Content.ReadFromJsonAsync<List<MenuCategoryListItemResponse>>();
        categories.ShouldNotBeNull();
        categories.ShouldContain(c => c.Id == category1!.Id);
        categories.ShouldNotContain(c => c.Id == category2.Id);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateCategory_Returns200_WithUpdatedData()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create
        var createResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Originele Naam", sortOrder: 1));
        var created = await createResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Update with new sort order, image, and translations
        var updateRequest = new
        {
            ImageUrl = "https://example.com/updated.jpg",
            SortOrder = 5,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Bijgewerkte Naam", Description = (string?)"Nieuwe beschrijving" },
                new { LanguageCode = "fr", Name = "Nom Mis à Jour", Description = (string?)null },
            }
        };

        var updateResponse = await client.PutAsJsonAsync(
            CategoryUrl(brand, created!.Id), updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();
        updated.ShouldNotBeNull();
        updated.ImageUrl.ShouldBe("https://example.com/updated.jpg");
        updated.SortOrder.ShouldBe(5);
        updated.Translations.Count.ShouldBe(2);
        updated.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Bijgewerkte Naam");
        updated.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Nom Mis à Jour");
    }

    [Fact]
    public async Task UpdateCategory_NonExistent_Returns404()
    {
        var client = CreateClient();
        var updateRequest = new
        {
            ImageUrl = (string?)null,
            SortOrder = 0,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Test", Description = (string?)null },
            }
        };

        var response = await client.PutAsJsonAsync(
            CategoryUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()), updateRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCategory_ShrinkTranslations_RemovesOldLanguage()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create with NL + FR
        var createRequest = new
        {
            SortOrder = 0,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Twee Talen", Description = (string?)null },
                new { LanguageCode = "fr", Name = "Deux Langues", Description = (string?)null },
            }
        };
        var createResponse = await client.PostAsJsonAsync(CategoriesUrl(brand), createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();
        created!.Translations.Count.ShouldBe(2);

        // Update to NL only — FR should be removed
        var updateRequest = new
        {
            ImageUrl = (string?)null,
            SortOrder = 0,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Eén Taal", Description = (string?)null },
            }
        };
        var updateResponse = await client.PutAsJsonAsync(
            CategoryUrl(brand, created.Id), updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();
        updated!.Translations.Count.ShouldBe(1);
        updated.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Eén Taal");
        updated.Translations.ShouldNotContain(t => t.LanguageCode == "fr");
    }

    // ── Delete (soft-delete) ──────────────────────────────────────────────────

    [Fact]
    public async Task DeleteCategory_Returns204_SoftDeletes()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create
        var createResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Te verwijderen"));
        var created = await createResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Delete
        var deleteResponse = await client.DeleteAsync(CategoryUrl(brand, created!.Id));
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Get by id should return 404 (soft-deleted, filtered by global query filter)
        var getResponse = await client.GetAsync(CategoryUrl(brand, created.Id));
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_NonExistent_Returns404()
    {
        var client = CreateClient();

        var response = await client.DeleteAsync(
            CategoryUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCategory_AlreadyDeleted_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create and delete
        var createResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Dubbel Verwijderen"));
        var created = await createResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();
        await client.DeleteAsync(CategoryUrl(brand, created!.Id));

        // Second delete should 404 (already filtered out)
        var secondDelete = await client.DeleteAsync(CategoryUrl(brand, created.Id));
        secondDelete.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Reorder ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReorderCategory_Returns204_UpdatesSortOrder()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create with sort order 1
        var createResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Herordenen Test", sortOrder: 1));
        var created = await createResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();
        created!.SortOrder.ShouldBe(1);

        // Reorder to sort order 99
        var reorderRequest = new { SortOrder = 99 };
        var reorderResponse = await client.PatchAsJsonAsync(
            ReorderUrl(brand, created.Id), reorderRequest);

        reorderResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify the updated sort order via GET
        var getResponse = await client.GetAsync(CategoryUrl(brand, created.Id));
        var updated = await getResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();
        updated!.SortOrder.ShouldBe(99);
    }

    [Fact]
    public async Task ReorderCategory_NonExistent_Returns404()
    {
        var client = CreateClient();
        var reorderRequest = new { SortOrder = 5 };

        var response = await client.PatchAsJsonAsync(
            ReorderUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()), reorderRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ReorderCategory_WithNegativeSortOrder_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a category first
        var createResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Reorder Validation Test"));
        var created = await createResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        var reorderRequest = new { SortOrder = -1 };
        var response = await client.PatchAsJsonAsync(
            ReorderUrl(brand, created!.Id), reorderRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── AssignProduct ─────────────────────────────────────────────────────────

    [Fact]
    public async Task AssignProductToCategory_Returns204()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a category
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Assign Test Category"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Create a product
        var createProductResponse = await client.PostAsJsonAsync(
            ProductsUrl(brand), MakeCreateProductRequest(nlName: "Assign Test Product"));
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Assign product to category
        var assignRequest = new
        {
            ProductId = product!.Id,
            CategoryId = category!.Id,
        };

        var assignResponse = await client.PostAsJsonAsync(AssignProductUrl(brand), assignRequest);

        assignResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task AssignProductToCategory_NonExistentCategory_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a product
        var createProductResponse = await client.PostAsJsonAsync(
            ProductsUrl(brand), MakeCreateProductRequest(nlName: "Assign 404 Category Test"));
        var product = await createProductResponse.Content.ReadFromJsonAsync<ProductResponse>();

        // Assign to non-existent category
        var assignRequest = new
        {
            ProductId = product!.Id,
            CategoryId = Guid.NewGuid(),
        };

        var assignResponse = await client.PostAsJsonAsync(AssignProductUrl(brand), assignRequest);

        assignResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignProductToCategory_NonExistentProduct_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a category
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateRequest(nlName: "Assign 404 Product Test"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Assign non-existent product
        var assignRequest = new
        {
            ProductId = Guid.NewGuid(),
            CategoryId = category!.Id,
        };

        var assignResponse = await client.PostAsJsonAsync(AssignProductUrl(brand), assignRequest);

        assignResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
