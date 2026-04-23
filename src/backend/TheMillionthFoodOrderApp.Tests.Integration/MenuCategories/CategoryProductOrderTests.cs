using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.MenuCategories;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.MenuCategories;

/// <summary>
/// Integration tests for US-FP-015: ordering products within a menu category.
/// Covers GET /menu-categories/{id}/products and PUT /menu-categories/{id}/products/order.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class CategoryProductOrderTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string CategoriesUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/menu-categories";

    private static string CategoryProductsUrl(string brandSlug, Guid categoryId) =>
        $"/api/brands/{brandSlug}/menu-categories/{categoryId}/products";

    private static string ReorderProductsUrl(string brandSlug, Guid categoryId) =>
        $"/api/brands/{brandSlug}/menu-categories/{categoryId}/products/order";

    private static string AssignProductUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/menu-categories/assign-product";

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static object MakeCreateCategoryRequest(string nlName = "Test Category", int sortOrder = 0) =>
        new
        {
            ImageUrl = (string?)null,
            SortOrder = sortOrder,
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = nlName, Description = (string?)"Test beschrijving" },
            }
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

    // ── GET /menu-categories/{id}/products ────────────────────────────────────

    [Test]
    public async Task GetCategoryProducts_ReturnsProductsSortedBySortOrderInCategory()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a category
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Order Test Category"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Create three products
        var createP1 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Product One"));
        var product1 = await createP1.Content.ReadFromJsonAsync<ProductResponse>();

        var createP2 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Product Two"));
        var product2 = await createP2.Content.ReadFromJsonAsync<ProductResponse>();

        var createP3 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Product Three"));
        var product3 = await createP3.Content.ReadFromJsonAsync<ProductResponse>();

        // Assign all three in order (each goes to the end)
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product1!.Id, CategoryId = category!.Id });
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product2!.Id, CategoryId = category.Id });
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product3!.Id, CategoryId = category.Id });

        // GET the products
        var getResponse = await client.GetAsync(CategoryProductsUrl(brand, category.Id));

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var products = await getResponse.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(products).IsNotNull();
        await Assert.That(products!.Count).IsEqualTo(3);

        // Verify ascending sort by SortOrderInCategory
        var index1 = products.FindIndex(p => p.Id == product1.Id);
        var index2 = products.FindIndex(p => p.Id == product2.Id);
        var index3 = products.FindIndex(p => p.Id == product3.Id);
        await Assert.That(index1).IsLessThan(index2);
        await Assert.That(index2).IsLessThan(index3);
    }

    [Test]
    public async Task GetCategoryProducts_EmptyCategory_ReturnsEmptyList()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a category with no products
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Empty Category"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        var getResponse = await client.GetAsync(CategoryProductsUrl(brand, category!.Id));

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var products = await getResponse.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(products).IsNotNull();
        await Assert.That(products!).IsEmpty();
    }

    [Test]
    public async Task GetCategoryProducts_NonExistentCategory_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            CategoryProductsUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetCategoryProducts_ReturnsProductsWithCorrectSortOrderValues()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        // Create a category
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Sort Value Category"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Create two products and assign them sequentially
        var createP1 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Sort P1"));
        var product1 = await createP1.Content.ReadFromJsonAsync<ProductResponse>();

        var createP2 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Sort P2"));
        var product2 = await createP2.Content.ReadFromJsonAsync<ProductResponse>();

        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product1!.Id, CategoryId = category!.Id });
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product2!.Id, CategoryId = category.Id });

        // GET the products and check SortOrderInCategory values
        var getResponse = await client.GetAsync(CategoryProductsUrl(brand, category.Id));
        var products = await getResponse.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(products).IsNotNull();

        var p1Result = products!.Single(p => p.Id == product1.Id);
        var p2Result = products.Single(p => p.Id == product2.Id);

        // First assigned product gets sort order 1 (0 + 1), second gets 2 (1 + 1)
        await Assert.That(p1Result.SortOrderInCategory).IsLessThan(p2Result.SortOrderInCategory);
    }

    // ── Assign product defaults to end of category ────────────────────────────

    [Test]
    public async Task AssignProduct_DefaultsToEndOfCategory()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a category
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Append Order Category"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Create and assign two products
        var createP1 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Append P1"));
        var product1 = await createP1.Content.ReadFromJsonAsync<ProductResponse>();

        var createP2 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Append P2"));
        var product2 = await createP2.Content.ReadFromJsonAsync<ProductResponse>();

        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product1!.Id, CategoryId = category!.Id });
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product2!.Id, CategoryId = category.Id });

        // List products and verify product2 appears after product1
        var getResponse = await client.GetAsync(CategoryProductsUrl(brand, category.Id));
        var products = await getResponse.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(products).IsNotNull();
        await Assert.That(products!.Count).IsEqualTo(2);

        var index1 = products.FindIndex(p => p.Id == product1.Id);
        var index2 = products.FindIndex(p => p.Id == product2.Id);
        await Assert.That(index1).IsLessThan(index2);
    }

    // ── PUT /menu-categories/{id}/products/order ──────────────────────────────

    [Test]
    public async Task ReorderProducts_Returns204_AndPersistsNewOrder()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a category
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Reorder Persist Category"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Create three products and assign them
        var createP1 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Reorder P1"));
        var product1 = await createP1.Content.ReadFromJsonAsync<ProductResponse>();

        var createP2 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Reorder P2"));
        var product2 = await createP2.Content.ReadFromJsonAsync<ProductResponse>();

        var createP3 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Reorder P3"));
        var product3 = await createP3.Content.ReadFromJsonAsync<ProductResponse>();

        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product1!.Id, CategoryId = category!.Id });
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product2!.Id, CategoryId = category.Id });
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product3!.Id, CategoryId = category.Id });

        // Reorder: submit reversed order [P3, P1, P2]
        var reorderRequest = new
        {
            ProductIds = new[] { product3!.Id, product1.Id, product2!.Id }
        };

        var reorderResponse = await client.PutAsJsonAsync(
            ReorderProductsUrl(brand, category.Id), reorderRequest);

        await Assert.That(reorderResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Verify new order persists via GET
        var getResponse = await client.GetAsync(CategoryProductsUrl(brand, category.Id));
        var products = await getResponse.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(products).IsNotNull();
        await Assert.That(products!.Count).IsEqualTo(3);

        var index3 = products.FindIndex(p => p.Id == product3.Id);
        var index1 = products.FindIndex(p => p.Id == product1.Id);
        var index2 = products.FindIndex(p => p.Id == product2.Id);

        // P3 should now be first, P1 second, P2 third
        await Assert.That(index3).IsLessThan(index1);
        await Assert.That(index1).IsLessThan(index2);
    }

    [Test]
    public async Task ReorderProducts_AssignsSequentialSortOrders()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        // Create a category
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Sequential Order Category"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Create two products and assign them
        var createP1 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Seq P1"));
        var product1 = await createP1.Content.ReadFromJsonAsync<ProductResponse>();

        var createP2 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Seq P2"));
        var product2 = await createP2.Content.ReadFromJsonAsync<ProductResponse>();

        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product1!.Id, CategoryId = category!.Id });
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product2!.Id, CategoryId = category.Id });

        // Reorder: [P2, P1]
        var reorderRequest = new
        {
            ProductIds = new[] { product2!.Id, product1.Id }
        };
        await client.PutAsJsonAsync(ReorderProductsUrl(brand, category.Id), reorderRequest);

        // P2 should have sort order 0, P1 should have sort order 1
        var getResponse = await client.GetAsync(CategoryProductsUrl(brand, category.Id));
        var products = await getResponse.Content.ReadFromJsonAsync<List<ProductListItemResponse>>();
        await Assert.That(products).IsNotNull();

        var p2Result = products!.Single(p => p.Id == product2.Id);
        var p1Result = products.Single(p => p.Id == product1.Id);

        await Assert.That(p2Result.SortOrderInCategory).IsEqualTo(0);
        await Assert.That(p1Result.SortOrderInCategory).IsEqualTo(1);
    }

    [Test]
    public async Task ReorderProducts_NonExistentCategory_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var reorderRequest = new
        {
            ProductIds = new[] { Guid.NewGuid() }
        };

        var response = await client.PutAsJsonAsync(
            ReorderProductsUrl(brand, Guid.NewGuid()), reorderRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task ReorderProducts_WithProductFromDifferentCategory_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create two categories
        var createCat1Response = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Category One"));
        var category1 = await createCat1Response.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        var createCat2Response = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Category Two"));
        var category2 = await createCat2Response.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Create products and assign them to different categories
        var createP1 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("CrossCat P1"));
        var product1 = await createP1.Content.ReadFromJsonAsync<ProductResponse>();

        var createP2 = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("CrossCat P2"));
        var product2 = await createP2.Content.ReadFromJsonAsync<ProductResponse>();

        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product1!.Id, CategoryId = category1!.Id });
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product2!.Id, CategoryId = category2!.Id });

        // Try to reorder category1 with product2 (which belongs to category2)
        var reorderRequest = new
        {
            ProductIds = new[] { product1.Id, product2.Id }
        };

        var response = await client.PutAsJsonAsync(
            ReorderProductsUrl(brand, category1.Id), reorderRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ReorderProducts_WithEmptyProductIds_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a category
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Validation Category"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        var reorderRequest = new
        {
            ProductIds = Array.Empty<Guid>()
        };

        var response = await client.PutAsJsonAsync(
            ReorderProductsUrl(brand, category!.Id), reorderRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ReorderProducts_WithDuplicateProductIds_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create a category
        var createCategoryResponse = await client.PostAsJsonAsync(
            CategoriesUrl(brand), MakeCreateCategoryRequest(nlName: "Duplicate IDs Category"));
        var category = await createCategoryResponse.Content.ReadFromJsonAsync<MenuCategoryResponse>();

        // Create a product and assign it
        var createP = await client.PostAsJsonAsync(ProductsUrl(brand), MakeCreateProductRequest("Dup Reorder P"));
        var product = await createP.Content.ReadFromJsonAsync<ProductResponse>();
        await client.PostAsJsonAsync(AssignProductUrl(brand),
            new { ProductId = product!.Id, CategoryId = category!.Id });

        // Submit with duplicate product ID
        var reorderRequest = new
        {
            ProductIds = new[] { product.Id, product.Id }
        };

        var response = await client.PutAsJsonAsync(
            ReorderProductsUrl(brand, category.Id), reorderRequest);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }
}
