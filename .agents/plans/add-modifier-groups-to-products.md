# Implementation Plan: US-FP-006 -- Add Modifier Groups to Products

## Overview

ModifierGroup as a new aggregate root with two levels of nested children (Modifiers, each with their own translations), connected to Products via a many-to-many join entity (`ProductModifierGroup`).

## Domain Model Design

### Key Design Decisions

1. **ModifierGroup is a separate aggregate root** -- not a child of Product. Modifier groups are shared across multiple products.
2. **Modifier is a child entity of ModifierGroup** -- same aggregate boundary. On update, modifiers are cleared and re-added.
3. **Many-to-many via ProductModifierGroup join entity** -- with `SortOrder` field for display order per product.
4. **Price adjustment uses plain decimal** -- `Money` value object rejects negatives, but modifiers can be negative. Currency is implicitly EUR.
5. **Both ModifierGroup and Modifier have translations** -- group name and modifier name each support NL, FR, DE.

### Entity Relationships

```
ModifierGroup (AggregateRoot<Guid>, IAuditable, ISoftDeletable)
 |-- ModifierGroupTranslation[] (name per language)
 |-- Modifier[] (child entities)
      |-- ModifierTranslation[] (name per language)
      |-- PriceAdjustment (decimal, can be negative)
      |-- SortOrder (int, display position within group)

ProductModifierGroup (Entity<Guid>, join entity)
 |-- ProductId (FK -> Products)
 |-- ModifierGroupId (FK -> ModifierGroups)
 |-- SortOrder (int, display order of this group on the product)
```

---

## Phase 1: Backend -- Domain Layer

New files in `Domain/ModifierGroups/`:
- `ModifierGroup.cs` -- aggregate root with Create/Update/SoftDelete
- `ModifierGroupTranslation.cs` -- translation entity
- `Modifier.cs` -- child entity with PriceAdjustment, SortOrder, Translations
- `ModifierTranslation.cs` -- modifier name translation
- `ProductModifierGroup.cs` -- join entity
- `ModifierGroupCreatedEvent.cs`, `ModifierGroupDeletedEvent.cs` -- domain events
- `IModifierGroupRepository.cs` -- repository interface

## Phase 2: Backend -- Infrastructure Layer

- 5 EF Core configuration files in `Infrastructure/ModifierGroups/`
- `ModifierGroupRepository.cs` with transactional three-level update
- Update `BrandDbContext.cs` with 5 new DbSets + query filter
- Update `DependencyInjection.cs` with repository registration
- Generate EF Core migration

## Phase 3: Backend -- Application Layer

- `ModifierGroupDtos.cs` -- request/response records
- `IModifierGroupService.cs` + `ModifierGroupService.cs`
- Update `DependencyInjection.cs` with service registration

## Phase 4: Backend -- API Endpoints (7 endpoints)

Routes:
- `POST /api/brands/{brandSlug}/modifier-groups` -- create
- `GET /api/brands/{brandSlug}/modifier-groups` -- list
- `GET /api/brands/{brandSlug}/modifier-groups/{id}` -- get
- `PUT /api/brands/{brandSlug}/modifier-groups/{id}` -- update
- `DELETE /api/brands/{brandSlug}/modifier-groups/{id}` -- delete
- `GET /api/brands/{brandSlug}/products/{productId}/modifier-groups` -- get product's groups
- `PUT /api/brands/{brandSlug}/products/{productId}/modifier-groups` -- set product's groups

## Phase 5: Backend -- Seed Data

Two modifier groups for Frietjes:
1. "Maat/Taille/Groesse" (Size): Klein +0, Medium +1, Groot +2
2. "Sauzen/Sauces/Sossen" (Sauces): Mayonaise +0, Stoofvleessaus +0.50, Speciaal +0.50

## Phase 6: Backend -- Tests

- Unit tests: 13+ tests for ModifierGroup domain logic (Shouldly)
- Integration tests: CRUD, product assignments, cross-brand isolation

## Phase 7: Frontend -- Types & API Client

- TypeScript interfaces in `types/common.ts`
- API client `api/modifierGroups.ts`

## Phase 8: Frontend -- TanStack Query Hooks

- `useModifierGroups.ts` with query keys, list/detail/mutations, product assignment hooks

## Phase 9: Frontend -- Admin Pages

- `ModifierGroupList.tsx` -- table with name, modifier count, product count
- `ModifierGroupCreate.tsx` -- form with translation tabs + dynamic modifier list
- `ModifierGroupEdit.tsx` -- edit form with pre-populated data
- Update `ProductEdit.tsx` -- add modifier groups assignment section
- Update `routes.tsx` -- add modifier-groups routes

## Phase 10: Frontend -- i18n

- Add `modifierGroups` keys to NL, FR, DE locale files

---

## Execution Order

1. Phase 1 (Domain) -- must be first
2. Phase 2 (Infrastructure) + Phase 7 (Frontend types) -- parallel
3. Phase 3 (Application) + Phase 6.1 (Unit tests) + Phase 8 (Frontend hooks) -- parallel
4. Phase 4 (API endpoints) + Phase 9 (Frontend pages) + Phase 10 (i18n) -- parallel
5. Phase 5 (Seed data) -- after API endpoints
6. Phase 6.2 (Integration tests) -- after API endpoints
