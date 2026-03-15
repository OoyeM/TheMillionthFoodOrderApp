# BFF (Backend-for-Frontend)

## Purpose
.NET project sitting between the React frontend and the API. Handles auth/session management.

## Identity Provider

- **Azure Entra External ID** (formerly Azure AD B2C) — CIAM service
- **Local accounts**: username/password or email/password registration
- **Social SSO**: Microsoft and Google sign-in
- Integrate via **Microsoft.Identity.Web** in the BFF layer
- BFF manages auth cookies/sessions; frontend never touches tokens directly
- Per-brand branding on login pages via Entra External ID customization

## Patterns
<!-- Add patterns as they emerge during development -->

## Gotchas
<!-- Add gotchas discovered during development -->
