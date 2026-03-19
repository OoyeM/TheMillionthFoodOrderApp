using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Application.ModifierGroups;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.ModifierGroups;

/// <summary>
/// Integration tests for modifier group CRUD operations.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
public sealed class ModifierGroupCrudTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ModifierGroupsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/modifier-groups";

    private static string ModifierGroupUrl(string brandSlug, Guid id) =>
        $"/api/brands/{brandSlug}/modifier-groups/{id}";

    /// <summary>
    /// Builds a valid create request with one group translation and one modifier.
    /// Optional overrides let individual tests deviate from defaults.
    /// </summary>
    private static object MakeCreateRequest(
        string nlName = "Sauzen",
        string? frName = null,
        object[]? modifiers = null) =>
        new
        {
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = nlName },
            }
            .Concat(frName is not null
                ? [new { LanguageCode = "fr", Name = frName }]
                : [])
            .ToArray(),
            Modifiers = modifiers ?? new[]
            {
                new
                {
                    PriceAdjustment = 0.50m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Mayonaise" } }
                },
            }
        };

    // ── Create ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateModifierGroup_Returns201_WithTranslationsAndModifiers()
    {
        var client = CreateClient();
        var request = MakeCreateRequest(
            nlName: "Sauzen",
            frName: "Sauces",
            modifiers:
            [
                new
                {
                    PriceAdjustment = 0.50m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Mayonaise" }, new { LanguageCode = "fr", Name = "Mayonnaise" } }
                },
                new
                {
                    PriceAdjustment = 0.75m,
                    SortOrder = 1,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Ketchup" }, new { LanguageCode = "fr", Name = "Ketchup" } }
                },
            ]);

        var response = await client.PostAsJsonAsync(
            ModifierGroupsUrl(IntegrationTestBase.AlphaSlug), request);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var group = await response.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        group.ShouldNotBeNull();
        group.Id.ShouldNotBe(Guid.Empty);
        group.Translations.Count.ShouldBe(2);
        group.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Sauzen");
        group.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Sauces");
        group.Modifiers.Count.ShouldBe(2);
        group.Modifiers.ShouldContain(m => m.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Mayonaise"));
        group.Modifiers.ShouldContain(m => m.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Ketchup"));
    }

    [Fact]
    public async Task CreateModifierGroup_WithoutTranslations_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            Translations = Array.Empty<object>(),
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = 0.50m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Mayonaise" } }
                },
            }
        };

        var response = await client.PostAsJsonAsync(
            ModifierGroupsUrl(IntegrationTestBase.AlphaSlug), request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateModifierGroup_WithInvalidLanguageCode_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            Translations = new[] { new { LanguageCode = "en", Name = "Sauces" } },
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = 0.50m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Mayonaise" } }
                },
            }
        };

        var response = await client.PostAsJsonAsync(
            ModifierGroupsUrl(IntegrationTestBase.AlphaSlug), request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateModifierGroup_WithDuplicateLanguageCodes_Returns400()
    {
        var client = CreateClient();
        var request = new
        {
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Sauzen 1" },
                new { LanguageCode = "nl", Name = "Sauzen 2" },
            },
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = 0.50m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Mayonaise" } }
                },
            }
        };

        var response = await client.PostAsJsonAsync(
            ModifierGroupsUrl(IntegrationTestBase.AlphaSlug), request);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Get ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetModifierGroup_Returns200_WithFullData()
    {
        var client = CreateClient();

        // Create first
        var createResponse = await client.PostAsJsonAsync(
            ModifierGroupsUrl(IntegrationTestBase.AlphaSlug),
            MakeCreateRequest(nlName: "Toppings", frName: "Garnitures"));
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();

        // Get by id
        var getResponse = await client.GetAsync(
            ModifierGroupUrl(IntegrationTestBase.AlphaSlug, created!.Id));

        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var group = await getResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        group.ShouldNotBeNull();
        group.Id.ShouldBe(created.Id);
        group.Translations.Count.ShouldBe(2);
        group.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Toppings");
        group.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Garnitures");
        group.Modifiers.Count.ShouldBe(1);
    }

    [Fact]
    public async Task GetModifierGroup_NonExistent_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            ModifierGroupUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── List ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListModifierGroups_ReturnsAll_ExcludesSoftDeleted()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        // Create two modifier groups
        var create1 = await client.PostAsJsonAsync(
            ModifierGroupsUrl(brand), MakeCreateRequest(nlName: "Groep A"));
        create1.StatusCode.ShouldBe(HttpStatusCode.Created);
        var group1 = await create1.Content.ReadFromJsonAsync<ModifierGroupResponse>();

        var create2 = await client.PostAsJsonAsync(
            ModifierGroupsUrl(brand), MakeCreateRequest(nlName: "Groep B"));
        create2.StatusCode.ShouldBe(HttpStatusCode.Created);
        var group2 = await create2.Content.ReadFromJsonAsync<ModifierGroupResponse>();

        // Soft-delete group1
        var deleteResponse = await client.DeleteAsync(ModifierGroupUrl(brand, group1!.Id));
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // List should contain group2 but not group1
        var listResponse = await client.GetAsync(ModifierGroupsUrl(brand));
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var groups = await listResponse.Content.ReadFromJsonAsync<List<ModifierGroupListItemResponse>>();
        groups.ShouldNotBeNull();
        groups.ShouldContain(g => g.Id == group2!.Id);
        groups.ShouldNotContain(g => g.Id == group1.Id);
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateModifierGroup_Returns200_WithUpdatedData()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create
        var createResponse = await client.PostAsJsonAsync(
            ModifierGroupsUrl(brand), MakeCreateRequest(nlName: "Originele Naam"));
        var created = await createResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();

        // Update with new translations and modifiers
        var updateRequest = new
        {
            Translations = new[]
            {
                new { LanguageCode = "nl", Name = "Bijgewerkte Naam" },
                new { LanguageCode = "fr", Name = "Nom Mis à Jour" },
            },
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = 1.00m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Truffelzout" } }
                },
            }
        };

        var updateResponse = await client.PutAsJsonAsync(
            ModifierGroupUrl(brand, created!.Id), updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        updated.ShouldNotBeNull();
        updated.Id.ShouldBe(created.Id);
        updated.Translations.Count.ShouldBe(2);
        updated.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Bijgewerkte Naam");
        updated.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Nom Mis à Jour");
        updated.Modifiers.Count.ShouldBe(1);
        updated.Modifiers.ShouldContain(m => m.Translations.Any(t => t.Name == "Truffelzout"));
    }

    [Fact]
    public async Task UpdateModifierGroup_CanChangeModifiers()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create with one modifier
        var createRequest = new
        {
            Translations = new[] { new { LanguageCode = "nl", Name = "Modifier Wissel Test" } },
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = 0.50m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Modifier 1" } }
                },
            }
        };
        var createResponse = await client.PostAsJsonAsync(ModifierGroupsUrl(brand), createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        created!.Modifiers.Count.ShouldBe(1);

        // Update with two different modifiers
        var updateRequest = new
        {
            Translations = new[] { new { LanguageCode = "nl", Name = "Modifier Wissel Test" } },
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = 1.00m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Nieuwe Modifier A" } }
                },
                new
                {
                    PriceAdjustment = 2.00m,
                    SortOrder = 1,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Nieuwe Modifier B" } }
                },
            }
        };

        var updateResponse = await client.PutAsJsonAsync(
            ModifierGroupUrl(brand, created.Id), updateRequest);

        updateResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        updated.ShouldNotBeNull();
        updated.Modifiers.Count.ShouldBe(2);
        updated.Modifiers.ShouldContain(m => m.Translations.Any(t => t.Name == "Nieuwe Modifier A"));
        updated.Modifiers.ShouldContain(m => m.Translations.Any(t => t.Name == "Nieuwe Modifier B"));
        // Original modifier should no longer exist
        updated.Modifiers.ShouldNotContain(m => m.Translations.Any(t => t.Name == "Modifier 1"));
    }

    [Fact]
    public async Task UpdateModifierGroup_NonExistent_Returns404()
    {
        var client = CreateClient();
        var updateRequest = new
        {
            Translations = new[] { new { LanguageCode = "nl", Name = "Bestaat Niet" } },
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = 0.50m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Modifier" } }
                },
            }
        };

        var response = await client.PutAsJsonAsync(
            ModifierGroupUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()), updateRequest);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Delete (soft-delete) ───────────────────────────────────────────────────

    [Fact]
    public async Task DeleteModifierGroup_Returns204_SoftDeletes()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Create
        var createResponse = await client.PostAsJsonAsync(
            ModifierGroupsUrl(brand), MakeCreateRequest(nlName: "Te Verwijderen Groep"));
        var created = await createResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();

        // Delete
        var deleteResponse = await client.DeleteAsync(ModifierGroupUrl(brand, created!.Id));
        deleteResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Get by id should return 404 (soft-deleted, filtered by global query filter)
        var getResponse = await client.GetAsync(ModifierGroupUrl(brand, created.Id));
        getResponse.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteModifierGroup_NonExistent_Returns404()
    {
        var client = CreateClient();

        var response = await client.DeleteAsync(
            ModifierGroupUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
