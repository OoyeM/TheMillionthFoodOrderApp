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

        /// <summary>OpenID Connect scheme for Keycloak (or any OIDC provider).</summary>
        public const string Oidc = "BffOidc";
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
    // Rate-limit policy names
    // -------------------------------------------------------------------------

    public static class RateLimitPolicies
    {
        /// <summary>Per-IP fixed-window limiter applied to <c>GET /bff/login</c>.</summary>
        public const string Login = "bff-login";
    }

    // -------------------------------------------------------------------------
    // Security header names + sentinel values
    // -------------------------------------------------------------------------

    public static class Headers
    {
        /// <summary>
        /// Custom header sent by the SPA on every state-changing call.
        /// Browsers cannot send custom headers cross-origin without a CORS
        /// preflight, so the presence of this header proves same-origin intent.
        /// </summary>
        public const string Csrf = "X-CSRF";

        /// <summary>The expected sentinel value for <see cref="Csrf"/>.</summary>
        public const string CsrfExpectedValue = "1";
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

        /// <summary>OIDC standard given-name claim (raw name; MapInboundClaims is disabled).</summary>
        public const string GivenName = "given_name";

        /// <summary>OIDC standard family-name claim.</summary>
        public const string FamilyName = "family_name";

        /// <summary>OIDC standard phone-number claim (from the "phone" scope), used to prefill checkout (US-FP-051).</summary>
        public const string PhoneNumber = "phone_number";
    }
}
