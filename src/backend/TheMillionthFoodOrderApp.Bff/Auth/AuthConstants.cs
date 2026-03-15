namespace TheMillionthFoodOrderApp.Bff.Auth;

/// <summary>
/// Central constants for authentication scheme names, role values, policy names,
/// and custom claim type names used across the BFF.
/// </summary>
public static class AuthConstants
{
    // -------------------------------------------------------------------------
    // Authentication scheme names
    // -------------------------------------------------------------------------

    public static class Schemes
    {
        /// <summary>The primary HttpOnly session cookie scheme.</summary>
        public const string Cookie = "BffCookie";

        /// <summary>Dev-only mock authentication scheme (never active in production).</summary>
        public const string Mock = "BffMock";
    }

    // -------------------------------------------------------------------------
    // Role constants — mirror the Domain StaffRole enum values as strings
    // -------------------------------------------------------------------------

    public static class Roles
    {
        public const string PlatformAdmin   = "PlatformAdmin";
        public const string BrandAdmin      = "BrandAdmin";
        public const string ShopManager     = "ShopManager";
        public const string CounterStaff    = "CounterStaff";
        public const string KitchenStaff    = "KitchenStaff";
        public const string FloorStaff      = "FloorStaff";
        public const string Customer        = "Customer";
    }

    // -------------------------------------------------------------------------
    // Authorization policy names
    // -------------------------------------------------------------------------

    public static class Policies
    {
        /// <summary>Any authenticated user.</summary>
        public const string RequireAuthenticated = "RequireAuthenticated";

        /// <summary>Any staff member (all roles except Customer).</summary>
        public const string RequireStaff = "RequireStaff";

        /// <summary>BrandAdmin or higher.</summary>
        public const string RequireBrandAdmin = "RequireBrandAdmin";

        /// <summary>PlatformAdmin only.</summary>
        public const string RequirePlatformAdmin = "RequirePlatformAdmin";
    }

    // -------------------------------------------------------------------------
    // Custom claim type names
    // -------------------------------------------------------------------------

    public static class Claims
    {
        /// <summary>Identifies the brand slug the user primarily belongs to.</summary>
        public const string BrandSlug = "brand_slug";

        /// <summary>
        /// Comma-separated list of brand-scoped roles in the format
        /// <c>{brandSlug}:{role}</c>, e.g. <c>frietjes:BrandAdmin</c>.
        /// </summary>
        public const string BrandRoles = "brand_roles";

        /// <summary>Platform-wide role (only set for PlatformAdmin users).</summary>
        public const string PlatformRole = "platform_role";
    }
}
