namespace TheMillionthFoodOrderApp.Domain.Identity;

/// <summary>
/// Staff roles within the platform hierarchy.
/// Brand-level roles (BrandAdmin, Customer) require BrandId but no ShopId.
/// Shop-level roles (ShopManager, CounterStaff, KitchenStaff, FloorStaff) require both BrandId and ShopId.
/// </summary>
public enum StaffRole
{
    BrandAdmin = 0,
    ShopManager = 1,
    CounterStaff = 2,
    KitchenStaff = 3,
    FloorStaff = 4,
    Customer = 5
}
