using Shouldly;
using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Tests.Unit.Products;

public sealed class ProductSortOrderTests
{
    private static readonly (string, string, string?)[] ValidTranslations =
    [
        ("nl", "Test Product", "Beschrijving"),
    ];

    // ── Default SortOrderInCategory ───────────────────────────────────────────

    [Fact]
    public void Create_DefaultSortOrderInCategory_IsZero()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        product.SortOrderInCategory.ShouldBe(0);
    }

    [Fact]
    public void Create_DefaultMenuCategoryId_IsNull()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        product.MenuCategoryId.ShouldBeNull();
    }

    // ── ReorderInCategory ─────────────────────────────────────────────────────

    [Fact]
    public void ReorderInCategory_SetsSortOrderInCategory()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        product.ReorderInCategory(5);

        product.SortOrderInCategory.ShouldBe(5);
    }

    [Fact]
    public void ReorderInCategory_UpdatesUpdatedAt()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var originalUpdatedAt = product.UpdatedAt;

        product.ReorderInCategory(3);

        product.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalUpdatedAt);
    }

    [Fact]
    public void ReorderInCategory_ToZero_SetsSortOrderToZero()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        product.ReorderInCategory(10);

        product.ReorderInCategory(0);

        product.SortOrderInCategory.ShouldBe(0);
    }

    [Fact]
    public void ReorderInCategory_DoesNotChangeMenCategoryId()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();
        product.AssignCategory(categoryId, 1);

        product.ReorderInCategory(99);

        product.MenuCategoryId.ShouldBe(categoryId);
    }

    // ── AssignCategory ────────────────────────────────────────────────────────

    [Fact]
    public void AssignCategory_SetsMenuCategoryId()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();

        product.AssignCategory(categoryId, 0);

        product.MenuCategoryId.ShouldBe(categoryId);
    }

    [Fact]
    public void AssignCategory_SetsSortOrderInCategory()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();

        product.AssignCategory(categoryId, 7);

        product.SortOrderInCategory.ShouldBe(7);
    }

    [Fact]
    public void AssignCategory_UpdatesUpdatedAt()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var originalUpdatedAt = product.UpdatedAt;
        var categoryId = Guid.CreateVersion7();

        product.AssignCategory(categoryId, 0);

        product.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalUpdatedAt);
    }

    [Fact]
    public void AssignCategory_ReplacesExistingCategoryAssignment()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var firstCategoryId = Guid.CreateVersion7();
        var secondCategoryId = Guid.CreateVersion7();
        product.AssignCategory(firstCategoryId, 1);

        product.AssignCategory(secondCategoryId, 5);

        product.MenuCategoryId.ShouldBe(secondCategoryId);
        product.SortOrderInCategory.ShouldBe(5);
    }

    // ── RemoveCategory ────────────────────────────────────────────────────────

    [Fact]
    public void RemoveCategory_ClearsMenuCategoryId()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();
        product.AssignCategory(categoryId, 3);

        product.RemoveCategory();

        product.MenuCategoryId.ShouldBeNull();
    }

    [Fact]
    public void RemoveCategory_ResetsSortOrderInCategoryToZero()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();
        product.AssignCategory(categoryId, 4);

        product.RemoveCategory();

        product.SortOrderInCategory.ShouldBe(0);
    }

    [Fact]
    public void RemoveCategory_UpdatesUpdatedAt()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var categoryId = Guid.CreateVersion7();
        product.AssignCategory(categoryId, 2);
        var updatedAtAfterAssign = product.UpdatedAt;

        product.RemoveCategory();

        product.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAtAfterAssign);
    }

    [Fact]
    public void RemoveCategory_WhenAlreadyUncategorised_ResetsSortOrderAndUpdatesTimestamp()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var beforeRemove = product.UpdatedAt;

        // RemoveCategory on an uncategorised product is a no-op for business state
        // but the method itself still updates UpdatedAt
        product.RemoveCategory();

        product.MenuCategoryId.ShouldBeNull();
        product.SortOrderInCategory.ShouldBe(0);
        product.UpdatedAt.ShouldBeGreaterThanOrEqualTo(beforeRemove);
    }
}
