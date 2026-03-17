# Plan: US-FP-070 — Database-per-brand Provisioning Hardening

## Overview

The `BrandDatabaseProvisioner` handles the happy path but has no error recovery, no post-provisioning verification, no health check, and no tests. Connection string derivation is duplicated in three places. This story hardens the provisioner for production readiness.

## Current State

- `BrandDatabaseProvisioner` is a Wolverine message handler triggered by `BrandCreatedEvent`
- Derives connection string, creates DB via DDL on `master`, applies EF Core migrations
- No try/catch, no retry, no verification after provisioning
- `DeriveBrandConnectionString` duplicated in: provisioner, `BrandDbContextFactory`, `IntegrationTestBase`
- No integration tests for the provisioner itself

## Phase 1: Centralize Connection String Derivation

### Step 1: Create `BrandConnectionStringHelper`
- **File:** `Infrastructure/Persistence/BrandConnectionStringHelper.cs` (new)
- Static helper with `DeriveBrandConnectionString(platformConnStr, slug)` and `DeriveMasterConnectionString(platformConnStr)`

### Step 2–4: Update consumers
- `BrandDatabaseProvisioner` → use helper
- `BrandDbContextFactory` → use helper
- `IntegrationTestBase` → use helper

## Phase 2: Error Handling and Retry

### Step 5: Structured error handling in `EnsureDatabaseExistsAsync`
- Catch `SqlException`, log error number + message
- Distinguish transient vs permanent failures

### Step 6: Structured error handling in `ApplyMigrationsAsync`
- Wrap `MigrateAsync` in try/catch, log at `Error`
- Add short delay after DB creation (SQL Server `CREATE DATABASE` is async internally)

### Step 7: Configure Wolverine retry policy
- In `Program.cs`, configure retry: up to 3 attempts with exponential backoff for `SqlException`
- Dead-letter after max retries (Wolverine native pattern, no Polly needed)

## Phase 3: Post-Provisioning Verification

### Step 8: Add `VerifyDatabaseProvisionedAsync`
- Query `sys.databases` to confirm brand DB exists
- Open connection to brand DB, query `__EFMigrationsHistory` to confirm all migrations applied
- Called at end of `HandleAsync`

## Phase 4: Health Check

### Step 9: Create `BrandDatabaseHealthCheck`
- **File:** `Infrastructure/Persistence/BrandDatabaseHealthCheck.cs` (new)
- Implements `IHealthCheck`
- Queries active brands, verifies each DB is reachable + migrations applied
- Cache results 60s to keep it lightweight

### Step 10: Register health check
- In `Program.cs`: `AddHealthChecks().AddCheck<BrandDatabaseHealthCheck>(...)`
- Aspire's `MapDefaultEndpoints()` picks it up automatically at `/health`

## Phase 5: Integration Tests

### Step 11: Create `BrandDatabaseProvisionerTests`
- **File:** `Tests.Integration/Provisioning/BrandDatabaseProvisionerTests.cs` (new)
- Tests:
  1. `Provisioner_CreatesBrandDatabase_WhenNotExists`
  2. `Provisioner_AppliesMigrations_ToNewDatabase`
  3. `Provisioner_IsIdempotent_WhenCalledTwice`
  4. `Provisioner_VerifiesDatabase_AfterProvisioning`
  5. `Provisioner_HandlesInvalidSlug_Gracefully`

## Files Summary

| File | Action |
|------|--------|
| `Infrastructure/Persistence/BrandConnectionStringHelper.cs` | Create |
| `Infrastructure/Persistence/BrandDatabaseProvisioner.cs` | Modify — use helper, add error handling, verification |
| `Infrastructure/Persistence/BrandDbContextFactory.cs` | Modify — use helper |
| `Infrastructure/Persistence/BrandDatabaseHealthCheck.cs` | Create |
| `Api/Program.cs` | Modify — Wolverine retry config, register health check |
| `Tests.Integration/Fixtures/IntegrationTestBase.cs` | Modify — use helper |
| `Tests.Integration/Provisioning/BrandDatabaseProvisionerTests.cs` | Create |

## Success Criteria
- [ ] Connection string derivation in one place only
- [ ] Provisioner logs structured errors on failure
- [ ] Wolverine retries transient failures up to 3 times
- [ ] Post-provisioning verification confirms DB + migrations
- [ ] Health check endpoint reports brand database status
- [ ] 5+ integration tests pass
- [ ] All existing tests still pass
