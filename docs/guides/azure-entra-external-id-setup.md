# Azure Entra External ID — Setup Guide

This guide walks through the full Azure portal setup for TheMillionthFoodOrderApp identity layer.
The application uses a **single Entra External ID tenant** for all brands. Per-brand branding is
achieved through separate app registrations (Option A) or custom authentication extensions (Option B).

> **You can build most of the codebase without doing any of this.** The BFF has a
> `MockAuthHandler` for local development. Set `Authentication:UseMockAuth=true` in
> `appsettings.Development.json` (already the default) and skip this guide until you need
> real logins. See the [Connecting to .NET](#9-connecting-to-the-net-project) section when ready.

---

## Table of Contents

1. [Create the Entra External ID Tenant](#1-create-the-entra-external-id-tenant)
2. [Register the BFF Application](#2-register-the-bff-application)
3. [Register the API Application](#3-register-the-api-application)
4. [Configure User Flows](#4-configure-user-flows)
5. [Configure Social Identity Providers](#5-configure-social-identity-providers)
6. [Per-Brand Branding](#6-per-brand-branding)
7. [Custom User Attributes](#7-custom-user-attributes)
8. [App Roles](#8-app-roles)
9. [Connecting to the .NET Project](#9-connecting-to-the-net-project)
10. [Testing Checklist](#10-testing-checklist)

---

## 1. Create the Entra External ID Tenant

**Important:** You need an **Entra External ID** tenant (CIAM), not a regular Entra ID / Azure AD
tenant. These are different products. Entra External ID is Microsoft's consumer-facing identity
platform, equivalent to the old Azure AD B2C.

### Steps

1. Open the [Azure Portal](https://portal.azure.com) and sign in with your Azure subscription.
2. Search for **"Microsoft Entra External ID"** in the top search bar. Do not select "Microsoft
   Entra ID" — that is the workforce identity product.
3. Click **"Create a tenant"**.
4. Select **"External"** as the tenant type.
5. Fill in the details:
   - **Organization name:** `TheMillionthFoodOrderApp`
   - **Domain name:** `themillionfoodorder` (becomes `themillionfoodorder.onmicrosoft.com`)
   - **Region:** West Europe (closest to Belgian users)
6. Click **Review + Create**, then **Create**.
7. Wait for provisioning to complete (~2 minutes), then click the link to switch into the new
   tenant.

> After creation, bookmark the tenant URL. All subsequent steps in this guide happen inside this
> External ID tenant, not your main Azure subscription tenant.

### Collect: Tenant Information

After switching into the new tenant, navigate to **Overview** and note:

| Value | Where to find it |
|-------|-----------------|
| **Tenant ID** | Overview page — "Tenant ID" field |
| **Primary domain** | Overview page — e.g. `themillionfoodorder.onmicrosoft.com` |

---

## 2. Register the BFF Application

The BFF is a confidential client (server-side ASP.NET Core) that handles the OIDC flow on behalf
of the browser. It holds the client secret and exchanges the authorization code for tokens.

### Steps

1. In the External ID tenant, navigate to **App registrations** > **New registration**.
2. Fill in:
   - **Name:** `TheMillionthFoodOrderApp-BFF`
   - **Supported account types:** Accounts in this organizational directory only
   - **Redirect URI:**
     - Platform: **Web**
     - URI: `https://localhost:7100/signin-oidc`
3. Click **Register**.

### Add Production Redirect URI

After registration, navigate to **Authentication** and add the production redirect URI:

```
https://bff.themillionfoodorderapp.com/signin-oidc
```

Add one URI per environment that needs OIDC (staging, production, etc.).

### Enable ID Tokens and Access Tokens

Still in **Authentication**, scroll to **Implicit grant and hybrid flows** and enable:

- [x] **Access tokens** (used by implicit flows — enable for hybrid PKCE support)
- [x] **ID tokens** (required for the `id_token` to be returned)

Click **Save**.

### Create a Client Secret

1. Navigate to **Certificates & secrets** > **Client secrets** > **New client secret**.
2. Description: `BFF client secret`
3. Expires: 24 months (set a calendar reminder to rotate before expiry)
4. Click **Add** and immediately copy the **Value** — it is only shown once.

**Never store this value in `appsettings.json` or commit it to source control.**
Store it in:
- Local development: `dotnet user-secrets` (see [Section 9](#9-connecting-to-the-net-project))
- Azure: Key Vault secret named `AzureAd--ClientSecret`

### Configure API Permissions

1. Navigate to **API permissions** > **Add a permission** > **Microsoft Graph** > **Delegated**.
2. Add all four of these permissions:
   - `openid`
   - `profile`
   - `email`
   - `offline_access`
3. Click **Add permissions**.
4. Click **Grant admin consent for [tenant name]** and confirm.

### Collect: BFF App Registration Values

| Value | Where to find it |
|-------|-----------------|
| **Client ID** (Application ID) | App registration Overview page |
| **Client Secret** | Certificates & secrets (copy immediately after creation) |
| **Tenant ID** | App registration Overview page |

---

## 3. Register the API Application

The API is the upstream resource server. It does not handle OIDC directly — the BFF acquires
tokens on behalf of the user and forwards them to the API as Bearer tokens.

### Steps

1. Navigate to **App registrations** > **New registration**.
2. Fill in:
   - **Name:** `TheMillionthFoodOrderApp-API`
   - **Supported account types:** Accounts in this organizational directory only
   - **Redirect URI:** Leave blank (APIs don't have redirect URIs)
3. Click **Register**.

### Expose an API — Define the Scope

1. Navigate to **Expose an API** > **Add a scope**.
2. If prompted, accept the default Application ID URI (`api://<client-id>`), or set a custom one
   like `api://themillionfoodorderapp-api`.
3. Fill in the scope:
   - **Scope name:** `access_as_user`
   - **Who can consent:** Admins and users
   - **Admin consent display name:** `Access TheMillionthFoodOrderApp API as user`
   - **Admin consent description:** `Allows the BFF to call the API on behalf of the signed-in user`
   - **User consent display name:** `Access TheMillionthFoodOrderApp API`
   - **State:** Enabled
4. Click **Add scope**.

The full scope URI will be something like:
```
api://themillionfoodorderapp-api/access_as_user
```

Note this URI — it goes into the BFF's OIDC scope configuration.

### Grant BFF Permission to This Scope

1. Go back to the **BFF app registration** (TheMillionthFoodOrderApp-BFF).
2. Navigate to **API permissions** > **Add a permission** > **My APIs**.
3. Select `TheMillionthFoodOrderApp-API`.
4. Select the `access_as_user` scope.
5. Click **Add permissions**.
6. Click **Grant admin consent** and confirm.

### Collect: API App Registration Values

| Value | Where to find it |
|-------|-----------------|
| **API Client ID** | API App registration Overview page |
| **Scope URI** | Expose an API > Scopes section |

---

## 4. Configure User Flows

User flows define the end-to-end sign-up/sign-in experience. Entra External ID provides
pre-built, configurable flows.

### 4.1 Combined Sign-Up / Sign-In Flow

1. In the External ID tenant, navigate to **User flows** > **New user flow**.
2. Select **Sign up and sign in**.
3. Name: `signup_signin` (Entra will prefix it to `B2C_1_signup_signin`)
4. Identity providers:
   - [x] Email signup
   - [x] Microsoft Account (add later in Section 5)
   - [x] Google (add later in Section 5)
5. User attributes to collect during sign-up:
   - [x] Given Name
   - [x] Surname
   - [x] Email Address
6. Token claims to return:
   - [x] Given Name
   - [x] Surname
   - [x] Email Addresses
   - [x] Identity Provider
   - [x] User's Object ID
   - [x] `preferred_language` (custom attribute — add after Section 7)
7. Click **Create**.

### 4.2 Password Reset Flow

1. Navigate to **User flows** > **New user flow**.
2. Select **Password reset**.
3. Name: `password_reset` (becomes `B2C_1_password_reset`)
4. Identity providers: Email verification
5. Claims: Email Addresses, User's Object ID
6. Click **Create**.

### 4.3 Profile Edit Flow

1. Navigate to **User flows** > **New user flow**.
2. Select **Profile editing**.
3. Name: `profile_edit` (becomes `B2C_1_profile_edit`)
4. User attributes that can be edited:
   - [x] Given Name
   - [x] Surname
   - [x] `preferred_language` (custom attribute — add after Section 7)
5. Token claims: same as sign-up/sign-in plus `preferred_language`
6. Click **Create**.

### Test a Flow

After creating each flow, click **Run user flow** to verify it opens a browser sign-in page
without errors before moving to the next step.

---

## 5. Configure Social Identity Providers

### 5.1 Google

#### Step A — Create Google OAuth 2.0 Credentials

1. Open [Google Cloud Console](https://console.cloud.google.com).
2. Create a new project (or use an existing one): `TheMillionthFoodOrderApp`.
3. Navigate to **APIs & Services** > **OAuth consent screen**:
   - User type: External
   - App name: `TheMillionthFoodOrderApp`
   - Support email: your email
   - Developer contact email: your email
   - Click **Save and Continue** through the remaining steps.
4. Navigate to **APIs & Services** > **Credentials** > **Create Credentials** > **OAuth client ID**:
   - Application type: **Web application**
   - Name: `Entra External ID`
   - Authorized redirect URIs: `https://themillionfoodorder.ciamlogin.com/themillionfoodorder.onmicrosoft.com/oauth2/authresp`
     (Replace with your actual External ID tenant domain.)
5. Click **Create** and copy the **Client ID** and **Client Secret**.

#### Step B — Register Google in Entra External ID

1. In the External ID tenant, navigate to **Identity providers** > **Google**.
2. Enter:
   - **Client ID:** (from Google Cloud Console)
   - **Client Secret:** (from Google Cloud Console)
3. Click **Save**.
4. Go back to your `B2C_1_signup_signin` user flow and enable Google as an identity provider.

### 5.2 Microsoft Account

Microsoft Account is simpler because it is a first-party Microsoft identity provider — no
separate OAuth app registration needed.

1. In the External ID tenant, navigate to **Identity providers** > **Microsoft Account**.
2. It uses the same tenant credentials. Click **Save**.
3. Go back to your `B2C_1_signup_signin` user flow and enable Microsoft Account.

---

## 6. Per-Brand Branding

The platform serves multiple restaurant brands (e.g. Frietjes?). Each brand has its own logo,
colors, and login page design. Two options exist:

### Option A — One App Registration Per Brand (Recommended)

Create a separate BFF app registration per brand following the same steps in
[Section 2](#2-register-the-bff-application), with a brand-specific name:

- `TheMillionthFoodOrderApp-BFF-Frietjes`
- `TheMillionthFoodOrderApp-BFF-[NextBrand]`

Each app registration gets its own **Company Branding** configuration:

1. Navigate to the brand-specific app registration.
2. Go to **Branding & properties**.
3. Upload the brand's:
   - Logo (PNG, max 245×36 px for banner logo)
   - Background image (PNG/JPG, max 1920×1080 px)
4. Set **Publisher display name** to the brand name.

The BFF uses a brand-specific Client ID in its configuration. The frontend selects the correct
Client ID based on the hostname or brand slug.

**Pros:** Full branding isolation, no custom policy complexity, straightforward authorization
scoping per brand.

**Cons:** One client secret to manage per brand. Rotation requires updating multiple secrets.
Automate this with Key Vault rotation policies.

### Option B — Single App Registration with Dynamic Branding

Use a single `TheMillionthFoodOrderApp-BFF` app registration and pass a `login_hint` or use
**Custom Authentication Extensions** to inject brand-specific branding via a claims provider
API call at login time.

This is significantly more complex. It requires:

- A custom authentication extension endpoint (Azure Function or API)
- Custom policies in Entra External ID
- Dynamic HTML/CSS injection via the extension

**Recommendation:** Start with Option A. Move to Option B only if operational overhead of
managing per-brand client secrets becomes a problem.

---

## 7. Custom User Attributes

Custom attributes let you store application-specific data on the user object in Entra.
The platform needs one custom attribute: `preferred_language`.

### Create the Attribute

1. In the External ID tenant, navigate to **Custom user attributes** > **Add**.
2. Fill in:
   - **Attribute name:** `preferred_language`
   - **Data type:** String
   - **Description:** Preferred language for this user (nl, fr, de)
3. Click **Add**.

### Add to User Flows

For each user flow created in Section 4:

1. Open the user flow.
2. Navigate to **User attributes** and enable `preferred_language` for collection.
3. Navigate to **Application claims** and enable `preferred_language` to be returned in tokens.
4. In the sign-up flow, optionally configure it as a dropdown with values `nl`, `fr`, `de`.

### Notes on Attribute Values

The attribute maps directly to the i18n language codes used by the frontend (`react-i18next`).
The BFF reads this claim and can pass it to the API via the `X-Preferred-Language` header or
enriched claims. Valid values: `nl`, `fr`, `de`.

---

## 8. App Roles

App roles define the authorization roles a user can be assigned. The platform role hierarchy is:

```
PlatformAdmin
BrandAdmin      (scoped to a brand in PlatformDb)
ShopManager     (scoped to a shop in PlatformDb)
CounterStaff    (scoped to a shop in PlatformDb)
KitchenStaff    (scoped to a shop in PlatformDb)
FloorStaff      (scoped to a shop in PlatformDb)
Customer        (scoped to a brand in PlatformDb)
```

> **Architecture note:** Brand and shop scoping is stored in `PlatformDb` (the `BrandUserRole`
> table), not in Entra custom attributes. This allows real-time role changes without Graph API
> calls. Entra app roles are used only for the top-level platform role (PlatformAdmin). All
> other role claims are enriched by the BFF's `ClaimsEnrichmentMiddleware` after login.

### Create App Roles on the API App Registration

1. Navigate to the `TheMillionthFoodOrderApp-API` app registration.
2. Go to **App roles** > **Create app role**.
3. Create one role for `PlatformAdmin`:
   - **Display name:** Platform Admin
   - **Allowed member types:** Users/Groups
   - **Value:** `PlatformAdmin`
   - **Description:** Full platform access across all brands
4. Create additional roles for the staff roles if you want Entra-level enforcement (optional —
   most role enforcement happens in PlatformDb):
   - `BrandAdmin`, `ShopManager`, `CounterStaff`, `KitchenStaff`, `FloorStaff`, `Customer`

### Assign Roles to Users

1. Navigate to **Enterprise applications** > `TheMillionthFoodOrderApp-API`.
2. Go to **Users and groups** > **Add user/group**.
3. Select the user and assign the `PlatformAdmin` role.

These role assignments flow into the `roles` claim in the access token received by the BFF.

---

## 9. Connecting to the .NET Project

### Configuration Values

Add the following section to `appsettings.json` in the BFF project
(`src/backend/TheMillionthFoodOrderApp.Bff/appsettings.json`):

```json
{
  "AzureAd": {
    "Instance": "https://themillionfoodorder.ciamlogin.com/",
    "TenantId": "<your-tenant-id>",
    "ClientId": "<bff-client-id>",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc",
    "ClientSecret": "DO NOT PUT HERE — use user-secrets or Key Vault",
    "Scopes": "openid profile email offline_access api://themillionfoodorderapp-api/access_as_user"
  },
  "Authentication": {
    "UseMockAuth": false,
    "Cookie": {
      "SlidingExpirationHours": 8
    }
  }
}
```

Replace the placeholder values:

| Placeholder | Value |
|-------------|-------|
| `<your-tenant-id>` | Tenant ID from Section 1 |
| `<bff-client-id>` | Client ID from Section 2 |
| `api://themillionfoodorderapp-api/access_as_user` | Scope URI from Section 3 |

The `Instance` URL format for Entra External ID tenants uses the CIAM login endpoint:
`https://<tenant-subdomain>.ciamlogin.com/`.

### Store the Client Secret in User Secrets

Never put the client secret in `appsettings.json`. Use `dotnet user-secrets` for local development:

```bash
# Navigate to the BFF project directory
cd src/backend/TheMillionthFoodOrderApp.Bff

# Initialize user-secrets for this project (if not already done)
dotnet user-secrets init

# Store the client secret
dotnet user-secrets set "AzureAd:ClientSecret" "<paste-secret-value-here>"
```

Verify the secret is stored:

```bash
dotnet user-secrets list
```

For production and staging, store the secret in Azure Key Vault as a secret named
`AzureAd--ClientSecret` (double dash maps to the colon separator). Configure the BFF to
read from Key Vault using the `Azure.Extensions.AspNetCore.Configuration.Secrets` package.

### Switch from Mock Auth to Real Auth

Mock authentication is controlled by a single configuration flag. To switch:

1. In `appsettings.Development.json`, set:
   ```json
   {
     "Authentication": {
       "UseMockAuth": false
     }
   }
   ```
2. Ensure the `AzureAd` configuration section is populated (see above).
3. Ensure `dotnet user-secrets` contains `AzureAd:ClientSecret`.
4. Restart the BFF. The startup log will no longer print the mock auth warning.

To go back to mock auth during development:

```json
{
  "Authentication": {
    "UseMockAuth": true
  }
}
```

Then use the mock login endpoint:
```
GET /bff/login?mock=brand-admin@frietjes&returnUrl=/frietjes/nl/admin
```

Available mock personas: `platform-admin`, `brand-admin@frietjes`, `counter-staff@frietjes`, `customer`.

### NuGet Packages Required (Phase 4)

When wiring the real OIDC flow, add these to the BFF project:

```bash
dotnet add package Microsoft.Identity.Web
dotnet add package Microsoft.Identity.Web.UI
```

These provide the `AddMicrosoftIdentityWebApp()` extension that configures OIDC with
`Microsoft.Identity.Web` conventions, including automatic token acquisition and refresh.

---

## 10. Testing Checklist

Work through these steps in order after completing the Azure setup.

### Tenant and App Registrations

- [ ] Can sign in to the External ID tenant in the Azure portal
- [ ] BFF app registration exists with redirect URI `https://localhost:7100/signin-oidc`
- [ ] API app registration exists with scope `access_as_user` exposed
- [ ] BFF has API permission granted for `access_as_user` with admin consent
- [ ] Client secret exists and has been stored in `dotnet user-secrets`

### User Flows

- [ ] `B2C_1_signup_signin` flow runs without errors (use "Run user flow" in portal)
- [ ] Can complete a full sign-up with email + password in the flow test runner
- [ ] `B2C_1_password_reset` flow runs without errors
- [ ] `B2C_1_profile_edit` flow runs without errors

### Social Identity Providers

- [ ] Google login option appears on the sign-in page
- [ ] Can sign in with a real Google account (creates a new user in Entra)
- [ ] Microsoft Account login option appears on the sign-in page
- [ ] Can sign in with a personal Microsoft Account

### BFF Integration

- [ ] BFF starts without errors with `UseMockAuth=false`
- [ ] `GET /bff/login` redirects to the Entra External ID login page
- [ ] After completing login, browser is redirected back to the BFF callback URL
- [ ] `GET /bff/user` returns `{ isAuthenticated: true, email: "...", ... }`
- [ ] `POST /bff/logout` clears the session cookie and returns 200
- [ ] `POST /bff/session/keepalive` returns 200 while session is active

### Authorization

- [ ] A user without the `PlatformAdmin` role gets 403 when calling a platform-admin-only endpoint
- [ ] Assigning the `PlatformAdmin` app role in Entra and logging in again grants access
- [ ] The `brand_roles` claim is enriched correctly by `ClaimsEnrichmentMiddleware` after login

### Per-Brand Branding (Option A)

- [ ] Each brand-specific app registration has Company Branding configured
- [ ] Logging in via a brand-specific client ID shows the correct brand logo and colors

### Custom Attribute

- [ ] `preferred_language` attribute is collected during sign-up
- [ ] The value `nl`, `fr`, or `de` appears in the token returned to the BFF
- [ ] The BFF passes the language preference through to the API or sets the `Accept-Language` header

---

## Reference: Azure Portal Checklist

Quick reference for the Azure portal operations in this guide:

| Task | Status |
|------|--------|
| Create Entra External ID tenant | [ ] |
| Register BFF app + redirect URIs | [ ] |
| Enable ID tokens + access tokens on BFF | [ ] |
| Create BFF client secret → store in Key Vault / user-secrets | [ ] |
| Grant BFF: openid, profile, email, offline_access | [ ] |
| Register API app | [ ] |
| Expose `access_as_user` scope on API app | [ ] |
| Grant BFF permission to `access_as_user` | [ ] |
| Create `B2C_1_signup_signin` user flow | [ ] |
| Create `B2C_1_password_reset` user flow | [ ] |
| Create `B2C_1_profile_edit` user flow | [ ] |
| Create Google OAuth credentials in Google Cloud Console | [ ] |
| Register Google as identity provider | [ ] |
| Register Microsoft Account as identity provider | [ ] |
| Configure Company Branding per brand app registration | [ ] |
| Create `preferred_language` custom attribute | [ ] |
| Add `preferred_language` to all user flows | [ ] |
| Create `PlatformAdmin` app role on API app | [ ] |
| Test full login flow end-to-end | [ ] |
