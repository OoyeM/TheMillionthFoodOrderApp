using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Application.ModifierGroups;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.ModifierGroups;

/// <summary>
/// Integration tests for assigning modifier groups to products.
/// Verifies that SET replaces existing assignments and that sort order is respected.
/// </summary>
public sealed class ProductModifierGroupTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static string ProductModifierGroupsUrl(string brandSlug, Guid productId) =>
        $"/api/brands/{brandSlug}/products/{productId}/modifier-groups";

    private static string ModifierGroupsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/modifier-groups";

    /// <summary>Creates a product and returns its id.</summary>
    private async Task<Guid> CreateProductAsync(HttpClient client, string brandSlug, string name = "Test Product")
    {
        var request = new
        {
            BasePrice = 3.50m,
            Translations = new[] { new { LanguageCode = "nl", Name = name, Description = (string?)null } }
        };
        var response = await client.PostAsJsonAsync(ProductsUrl(brandSlug), request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        return product!.Id;
    }

    /// <summary>Creates a modifier group and returns its id.</summary>
    private async Task<Guid> CreateModifierGroupAsync(HttpClient client, string brandSlug, string name = "Test Groep")
    {
        var request = new
        {
            Translations = new[] { new { LanguageCode = "nl", Name = name } },
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = 0.50m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Optie" } }
                },
            }
        };
        var response = await client.PostAsJsonAsync(ModifierGroupsUrl(brandSlug), request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var group = await response.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        return group!.Id;
    }

    // ── Set assignments ────────────────────────────────────────────────────────

    [Fact]
    public async Task SetProductModifierGroups_Returns200_AssignsGroups()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var productId = await CreateProductAsync(client, brand, "Product met Sauzen");
        var groupId1 = await CreateModifierGroupAsync(client, brand, "Sauzen");
        var groupId2 = await CreateModifierGroupAsync(client, brand, "Extra's");

        var request = new
        {
            Assignments = new[]
            {
                new { ModifierGroupId = groupId1, SortOrder = 0 },
                new { ModifierGroupId = groupId2, SortOrder = 1 },
            }
        };

        var response = await client.PutAsJsonAsync(
            ProductModifierGroupsUrl(brand, productId), request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var assignments = await response.Content.ReadFromJsonAsync<List<ProductModifierGroupResponse>>();
        assignments.ShouldNotBeNull();
        assignments.Count.ShouldBe(2);
        assignments.ShouldContain(a => a.ModifierGroupId == groupId1 && a.SortOrder == 0);
        assignments.ShouldContain(a => a.ModifierGroupId == groupId2 && a.SortOrder == 1);
        assignments.All(a => a.ProductId == productId).ShouldBeTrue();
    }

    [Fact]
    public async Task GetProductModifierGroups_ReturnsGroupsOrderedBySortOrder()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var productId = await CreateProductAsync(client, brand, "Sorteer Test Product");
        var groupIdA = await CreateModifierGroupAsync(client, brand, "Groep A");
        var groupIdB = await CreateModifierGroupAsync(client, brand, "Groep B");
        var groupIdC = await CreateModifierGroupAsync(client, brand, "Groep C");

        // Assign in non-trivial sort order
        var setRequest = new
        {
            Assignments = new[]
            {
                new { ModifierGroupId = groupIdC, SortOrder = 2 },
                new { ModifierGroupId = groupIdA, SortOrder = 0 },
                new { ModifierGroupId = groupIdB, SortOrder = 1 },
            }
        };
        await client.PutAsJsonAsync(ProductModifierGroupsUrl(brand, productId), setRequest);

        // Get and verify ordering
        var getResponse = await client.GetAsync(ProductModifierGroupsUrl(brand, productId));
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var assignments = await getResponse.Content.ReadFromJsonAsync<List<ProductModifierGroupResponse>>();
        assignments.ShouldNotBeNull();
        assignments.Count.ShouldBe(3);

        // Verify the items appear in ascending sort order
        assignments[0].ModifierGroupId.ShouldBe(groupIdA);
        assignments[0].SortOrder.ShouldBe(0);
        assignments[1].ModifierGroupId.ShouldBe(groupIdB);
        assignments[1].SortOrder.ShouldBe(1);
        assignments[2].ModifierGroupId.ShouldBe(groupIdC);
        assignments[2].SortOrder.ShouldBe(2);
    }

    [Fact]
    public async Task SetProductModifierGroups_ReplacesExistingAssignments()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var productId = await CreateProductAsync(client, brand, "Vervang Assignments Product");
        var groupId1 = await CreateModifierGroupAsync(client, brand, "Originele Groep");
        var groupId2 = await CreateModifierGroupAsync(client, brand, "Vervangende Groep");

        // Assign groupId1
        var firstSet = new
        {
            Assignments = new[] { new { ModifierGroupId = groupId1, SortOrder = 0 } }
        };
        await client.PutAsJsonAsync(ProductModifierGroupsUrl(brand, productId), firstSet);

        // Replace with groupId2
        var secondSet = new
        {
            Assignments = new[] { new { ModifierGroupId = groupId2, SortOrder = 0 } }
        };
        var replaceResponse = await client.PutAsJsonAsync(
            ProductModifierGroupsUrl(brand, productId), secondSet);

        replaceResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var assignments = await replaceResponse.Content.ReadFromJsonAsync<List<ProductModifierGroupResponse>>();
        assignments.ShouldNotBeNull();
        assignments.Count.ShouldBe(1);
        assignments.ShouldContain(a => a.ModifierGroupId == groupId2);
        // Original groupId1 assignment must be gone
        assignments.ShouldNotContain(a => a.ModifierGroupId == groupId1);
    }

    [Fact]
    public async Task SetProductModifierGroups_WithEmptyList_RemovesAll()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var productId = await CreateProductAsync(client, brand, "Leeg Assignments Product");
        var groupId = await CreateModifierGroupAsync(client, brand, "Te Verwijderen Groep");

        // Assign a group first
        var setRequest = new
        {
            Assignments = new[] { new { ModifierGroupId = groupId, SortOrder = 0 } }
        };
        await client.PutAsJsonAsync(ProductModifierGroupsUrl(brand, productId), setRequest);

        // Send empty list — should remove all assignments
        var clearRequest = new { Assignments = Array.Empty<object>() };
        var clearResponse = await client.PutAsJsonAsync(
            ProductModifierGroupsUrl(brand, productId), clearRequest);

        clearResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var assignments = await clearResponse.Content.ReadFromJsonAsync<List<ProductModifierGroupResponse>>();
        assignments.ShouldNotBeNull();
        assignments.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetProductModifierGroups_WithDuplicateGroupIds_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var productId = await CreateProductAsync(client, brand, "Dubbele Groep Product");
        var groupId = await CreateModifierGroupAsync(client, brand, "Dubbele Groep");

        var request = new
        {
            Assignments = new[]
            {
                new { ModifierGroupId = groupId, SortOrder = 0 },
                new { ModifierGroupId = groupId, SortOrder = 1 },  // duplicate
            }
        };

        var response = await client.PutAsJsonAsync(
            ProductModifierGroupsUrl(brand, productId), request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Get assignments ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProductModifierGroups_ForProductWithNoGroups_ReturnsEmptyList()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var productId = await CreateProductAsync(client, brand, "Product Zonder Groepen");

        var getResponse = await client.GetAsync(ProductModifierGroupsUrl(brand, productId));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var assignments = await getResponse.Content.ReadFromJsonAsync<List<ProductModifierGroupResponse>>();
        assignments.ShouldNotBeNull();
        assignments.ShouldBeEmpty();
    }
}
