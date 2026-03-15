# PR #76 — Review Issues Below Threshold

Issues identified during code review that scored below 80 confidence. Listed for manual review.

## 1. YARP X-Brand-Slug middleware is a no-op (score: 75)

**File:** `src/backend/TheMillionthFoodOrderApp.Bff/Program.cs`

The YARP proxy pipeline reads the `X-Brand-Slug` header and writes it back unchanged:

```csharp
if (context.Request.Headers.TryGetValue("X-Brand-Slug", out var brandSlug))
    context.Request.Headers["X-Brand-Slug"] = brandSlug;
```

YARP forwards all request headers by default, so this does nothing. It was likely intended to inject/validate the slug from the authenticated user's claims to prevent tenant isolation bypass. A client could send any brand slug and the API would trust it.

---

## 2. AllowAnonymous on ConfigureStaffAuthEndpoint (score: 75)

**File:** `src/backend/TheMillionthFoodOrderApp.Api/Endpoints/Brands/ConfigureStaffAuthEndpoint.cs`

`PUT /api/brands/{slug}/staff-auth` uses `AllowAnonymous()`. This is documented as intentional for dev (all endpoints are AllowAnonymous currently), but this endpoint modifies sensitive brand auth configuration. When production auth is wired, this should require BrandAdmin or PlatformAdmin.

---

## 3. Unique index filter excludes null ShopId (score: 75)

**File:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Identity/BrandUserRoleConfiguration.cs`

The unique index on `BrandUserRoles(PlatformUserId, BrandId, ShopId, Role)` has filter `[ShopId] IS NOT NULL`. SQL Server treats each NULL as distinct, so brand-level roles (where ShopId is null) are not enforced as unique at the DB level. A user could get duplicate BrandAdmin assignments for the same brand if application-level validation is bypassed. The `IdentityService.AssignRoleAsync` guards against this, but the database provides no safety net.

---

## 4. OccurredOnUtc renamed to OccurredOn (score: 75)

**File:** `src/backend/TheMillionthFoodOrderApp.Domain/Common/IDomainEvent.cs`

PR #74 specifically added the `Utc` suffix to signal that domain events always use UTC. This PR renames it back to `OccurredOn`. Since `DateTimeOffset` doesn't enforce UTC, the suffix was the only contract signal. If any Wolverine handler references `.OccurredOnUtc`, it will break. The codebase is young so this is likely safe, but worth a grep to confirm.

---

## 5. BffAuthProvider network errors treated as unauthenticated (score: 65)

**File:** `src/frontend/src/auth/BffAuthProvider.tsx`

The `useQuery` for `/bff/user` has `retry: false` and never destructures `isError`. When the query fails (network error, 500, CORS), `user` defaults to null and `isLoading` becomes false. The app silently treats any error as "not authenticated" rather than showing an error/loading state. On a flaky network, users see a login prompt instead of an error message.

---

## 6. AuditSaveChangesInterceptor singleton never used (score: 50)

**Files:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/DependencyInjection.cs` + `src/backend/TheMillionthFoodOrderApp.Api/Program.cs`

`DependencyInjection.cs` registers `AuditSaveChangesInterceptor` as a singleton. `Program.cs` creates a new instance with `new AuditSaveChangesInterceptor()` in the `configureDbContextOptions` callback. The DI-registered singleton is dead code. Either remove the singleton registration or resolve it from DI instead of using `new`.

---

## 7. BrandContextAccessor XML doc claims "thread-safe" (score: 25)

**File:** `src/backend/TheMillionthFoodOrderApp.Infrastructure/Multitenancy/BrandContextAccessor.cs`

XML doc says "Thread-safe, request-scoped holder" but it's a plain auto-property with no synchronization. Since it's scoped (one per request), concurrent access per instance is unlikely in practice. The comment is misleading but not a functional issue.
