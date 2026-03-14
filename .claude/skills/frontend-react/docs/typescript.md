# TypeScript Conventions

## Strict Mode

- `strict: true` in tsconfig — no exceptions
- **No `any`** — use `unknown` and narrow, or define proper types
- Enable `noUncheckedIndexedAccess` for safer array/object access

## Type Organization

- Co-locate types with the feature that owns them
- Shared types in `src/types/` — only for cross-feature contracts
- API response types auto-generated from OpenAPI spec when available

```
features/
  products/
    types.ts          ← product-specific types
    components/
    hooks/
src/types/
  api.ts              ← shared API contracts
  common.ts           ← Brand, Shop, User, etc.
```

## Naming

- **Interfaces** for object shapes: `interface ProductCardProps`
- **Type aliases** for unions, intersections, utilities: `type OrderStatus = 'pending' | 'confirmed' | 'ready'`
- Suffix props interfaces with `Props`: `ProductCardProps`
- Suffix API response types with `Response`/`Request`: `CreateOrderRequest`

## Patterns

```tsx
// Discriminated unions for state
type OrderState =
  | { status: 'loading' }
  | { status: 'error'; error: Error }
  | { status: 'success'; order: Order };

// Const assertions for enum-like values
const ORDER_STATUSES = ['pending', 'confirmed', 'preparing', 'ready', 'delivered'] as const;
type OrderStatus = (typeof ORDER_STATUSES)[number];

// Utility types
type PartialBy<T, K extends keyof T> = Omit<T, K> & Partial<Pick<T, K>>;
```

## Domain Types

Key types reflecting the domain model:

```tsx
// Multi-tenant hierarchy
interface Brand { id: string; name: string; slug: string; }
interface Shop { id: string; brandId: string; name: string; }

// Products with modifiers
interface Product {
  id: string;
  name: LocalizedString;  // { nl: string; fr: string; de: string }
  price: Money;
  allergens: Allergen[];
  type: 'simple' | 'with-modifiers' | 'combo';
}

// Money as value object (never raw numbers for prices)
interface Money { amount: number; currency: 'EUR'; }

// Localized strings
type SupportedLocale = 'nl' | 'fr' | 'de';
type LocalizedString = Record<SupportedLocale, string>;
```

## Strict Rules

- No type assertions (`as`) unless absolutely necessary — add a comment explaining why
- No non-null assertions (`!`) — handle the null case
- Prefer `satisfies` over `as` for type-safe object literals
- Use `unknown` for catch blocks: `catch (error: unknown)`
