using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.ModifierGroups;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.ModifierGroups;

/// <summary>
/// Integration tests for modifier group CRUD operations.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class ModifierGroupCrudTests(IntegrationTestBase fixture)
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

    [Test]
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var group = await response.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        await Assert.That(group).IsNotNull();
        await Assert.That(group!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(group.Translations.Count).IsEqualTo(2);
        await Assert.That(group.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Sauzen")).IsTrue();
        await Assert.That(group.Translations.Any(t => t.LanguageCode == "fr" && t.Name == "Sauces")).IsTrue();
        await Assert.That(group.Modifiers.Count).IsEqualTo(2);
        await Assert.That(group.Modifiers.Any(m => m.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Mayonaise"))).IsTrue();
        await Assert.That(group.Modifiers.Any(m => m.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Ketchup"))).IsTrue();
    }

    [Test]
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── Get ────────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetModifierGroup_Returns200_WithFullData()
    {
        var client = CreateClient();

        // Create first
        var createResponse = await client.PostAsJsonAsync(
            ModifierGroupsUrl(IntegrationTestBase.AlphaSlug),
            MakeCreateRequest(nlName: "Toppings", frName: "Garnitures"));
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();

        // Get by id
        var getResponse = await client.GetAsync(
            ModifierGroupUrl(IntegrationTestBase.AlphaSlug, created!.Id));

        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var group = await getResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        await Assert.That(group).IsNotNull();
        await Assert.That(group!.Id).IsEqualTo(created.Id);
        await Assert.That(group.Translations.Count).IsEqualTo(2);
        await Assert.That(group.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Toppings")).IsTrue();
        await Assert.That(group.Translations.Any(t => t.LanguageCode == "fr" && t.Name == "Garnitures")).IsTrue();
        await Assert.That(group.Modifiers.Count).IsEqualTo(1);
    }

    [Test]
    public async Task GetModifierGroup_NonExistent_Returns404()
    {
        var client = CreateClient();

        var response = await client.GetAsync(
            ModifierGroupUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── List ───────────────────────────────────────────────────────────────────

    [Test]
    public async Task ListModifierGroups_ReturnsAll_ExcludesSoftDeleted()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        // Create two modifier groups
        var create1 = await client.PostAsJsonAsync(
            ModifierGroupsUrl(brand), MakeCreateRequest(nlName: "Groep A"));
        await Assert.That(create1.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var group1 = await create1.Content.ReadFromJsonAsync<ModifierGroupResponse>();

        var create2 = await client.PostAsJsonAsync(
            ModifierGroupsUrl(brand), MakeCreateRequest(nlName: "Groep B"));
        await Assert.That(create2.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var group2 = await create2.Content.ReadFromJsonAsync<ModifierGroupResponse>();

        // Soft-delete group1
        var deleteResponse = await client.DeleteAsync(ModifierGroupUrl(brand, group1!.Id));
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // List should contain group2 but not group1
        var listResponse = await client.GetAsync(ModifierGroupsUrl(brand));
        await Assert.That(listResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var groups = await listResponse.Content.ReadFromJsonAsync<List<ModifierGroupListItemResponse>>();
        await Assert.That(groups).IsNotNull();
        await Assert.That(groups!.Any(g => g.Id == group2!.Id)).IsTrue();
        await Assert.That(groups.Any(g => g.Id == group1.Id)).IsFalse();
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    [Test]
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

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Id).IsEqualTo(created.Id);
        await Assert.That(updated.Translations.Count).IsEqualTo(2);
        await Assert.That(updated.Translations.Any(t => t.LanguageCode == "nl" && t.Name == "Bijgewerkte Naam")).IsTrue();
        await Assert.That(updated.Translations.Any(t => t.LanguageCode == "fr" && t.Name == "Nom Mis à Jour")).IsTrue();
        await Assert.That(updated.Modifiers.Count).IsEqualTo(1);
        await Assert.That(updated.Modifiers.Any(m => m.Translations.Any(t => t.Name == "Truffelzout"))).IsTrue();
    }

    [Test]
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
        await Assert.That(created!.Modifiers.Count).IsEqualTo(1);

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

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.Modifiers.Count).IsEqualTo(2);
        await Assert.That(updated.Modifiers.Any(m => m.Translations.Any(t => t.Name == "Nieuwe Modifier A"))).IsTrue();
        await Assert.That(updated.Modifiers.Any(m => m.Translations.Any(t => t.Name == "Nieuwe Modifier B"))).IsTrue();
        // Original modifier should no longer exist
        await Assert.That(updated.Modifiers.Any(m => m.Translations.Any(t => t.Name == "Modifier 1"))).IsFalse();
    }

    [Test]
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── Delete (soft-delete) ───────────────────────────────────────────────────

    [Test]
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
        await Assert.That(deleteResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Get by id should return 404 (soft-deleted, filtered by global query filter)
        var getResponse = await client.GetAsync(ModifierGroupUrl(brand, created.Id));
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteModifierGroup_NonExistent_Returns404()
    {
        var client = CreateClient();

        var response = await client.DeleteAsync(
            ModifierGroupUrl(IntegrationTestBase.AlphaSlug, Guid.NewGuid()));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
