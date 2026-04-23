using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Tests.Unit.Products;

public sealed class ProductSortOrderTests
{
    private static readonly (string, string, string?)[] ValidTranslations =
    [
        ("nl", "Test Product", "Beschrijving"),
    ];

    // ── Default SortOrderInCategory ───────────────────────────────────────────

    [Test]
    public async Task Create_DefaultSortOrderInCategory_IsZero()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        await Assert.That(product.SortOrderInCategory).IsEqualTo(0);
    }

    [Test]
    public async Task Create_DefaultMenuCategoryId_IsNull()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        await Assert.That(product.MenuCategoryId).IsNull();
    }

    // ── ReorderInCategory ─────────────────────────────────────────────────────

    [Test]
    public async Task ReorderInCategory_SetsSortOrderInCategory()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        product.ReorderInCategory(5);

        await Assert.That(product.SortOrderInCategory).IsEqualTo(5);
    }

    [Test]
    public async Task ReorderInCategory_UpdatesUpdatedAt()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var originalUpdatedAt = product.UpdatedAt;

        product.ReorderInCategory(3);

        await Assert.That(product.UpdatedAt).IsGreaterThanOrEqualTo(originalUpdatedAt);
    }

    [Test]
    public async Task ReorderInCategory_ToZero_SetsSortOrderToZero()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        product.ReorderInCategory(10);

        product.ReorderInCategory(0);

        await Assert.That(product.SortOrderInCategory).IsEqualTo(0);
    }

    [Test]
    public async Task ReorderInCategory_DoesNotChangeMenCategoryId()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();
        product.AssignCategory(categoryId, 1);

        product.ReorderInCategory(99);

        await Assert.That(product.MenuCategoryId).IsEqualTo(categoryId);
    }

    // ── AssignCategory ────────────────────────────────────────────────────────

    [Test]
    public async Task AssignCategory_SetsMenuCategoryId()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();

        product.AssignCategory(categoryId, 0);

        await Assert.That(product.MenuCategoryId).IsEqualTo(categoryId);
    }

    [Test]
    public async Task AssignCategory_SetsSortOrderInCategory()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();

        product.AssignCategory(categoryId, 7);

        await Assert.That(product.SortOrderInCategory).IsEqualTo(7);
    }

    [Test]
    public async Task AssignCategory_UpdatesUpdatedAt()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var originalUpdatedAt = product.UpdatedAt;
        var categoryId = Guid.CreateVersion7();

        product.AssignCategory(categoryId, 0);

        await Assert.That(product.UpdatedAt).IsGreaterThanOrEqualTo(originalUpdatedAt);
    }

    [Test]
    public async Task AssignCategory_ReplacesExistingCategoryAssignment()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var firstCategoryId = Guid.CreateVersion7();
        var secondCategoryId = Guid.CreateVersion7();
        product.AssignCategory(firstCategoryId, 1);

        product.AssignCategory(secondCategoryId, 5);

        await Assert.That(product.MenuCategoryId).IsEqualTo(secondCategoryId);
        await Assert.That(product.SortOrderInCategory).IsEqualTo(5);
    }

    // ── RemoveCategory ────────────────────────────────────────────────────────

    [Test]
    public async Task RemoveCategory_ClearsMenuCategoryId()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();
        product.AssignCategory(categoryId, 3);

        product.RemoveCategory();

        await Assert.That(product.MenuCategoryId).IsNull();
    }

    [Test]
    public async Task RemoveCategory_ResetsSortOrderInCategoryToZero()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();
        product.AssignCategory(categoryId, 4);

        product.RemoveCategory();

        await Assert.That(product.SortOrderInCategory).IsEqualTo(0);
    }

    [Test]
    public async Task RemoveCategory_UpdatesUpdatedAt()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();
        product.AssignCategory(categoryId, 2);
        var updatedAtAfterAssign = product.UpdatedAt;

        product.RemoveCategory();

        await Assert.That(product.UpdatedAt).IsGreaterThanOrEqualTo(updatedAtAfterAssign);
    }

    [Test]
    public async Task RemoveCategory_WhenAlreadyUncategorised_ResetsSortOrderAndUpdatesTimestamp()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var beforeRemove = product.UpdatedAt;

        // RemoveCategory on an uncategorised product is a no-op for business state
        // but the method itself still updates UpdatedAt
        product.RemoveCategory();

        await Assert.That(product.MenuCategoryId).IsNull();
        await Assert.That(product.SortOrderInCategory).IsEqualTo(0);
        await Assert.That(product.UpdatedAt).IsGreaterThanOrEqualTo(beforeRemove);
    }
}
