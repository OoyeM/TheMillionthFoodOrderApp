# React Patterns

## Component Conventions

- **Functional components only** — no class components
- **Named exports** — `export function ProductCard()` not `export default`
- Co-locate styles, tests, and types with the component
- Feature-based folder structure: `features/<feature>/components/`, `features/<feature>/hooks/`

## Component Structure

```tsx
// 1. Imports (external → internal → types)
// 2. Types/interfaces
// 3. Component
// 4. Sub-components (if small and tightly coupled)

interface ProductCardProps {
  product: Product;
  onAddToCart: (productId: string) => void;
}

export function ProductCard({ product, onAddToCart }: ProductCardProps) {
  // hooks first
  const { t } = useTranslation();
  const [quantity, setQuantity] = useState(1);

  // derived state
  const totalPrice = product.price * quantity;

  // handlers
  function handleAdd() {
    onAddToCart(product.id);
  }

  // render
  return ( /* ... */ );
}
```

## Hooks

- Extract reusable logic into custom hooks: `use<Feature><Action>`
- Keep hooks focused — one concern per hook
- Hooks that call the API should use TanStack Query (see tanstack-query.md)

```tsx
// features/cart/hooks/useCart.ts
export function useCart() {
  const { data: cart } = useCartQuery();
  const addItem = useAddCartItemMutation();
  const removeItem = useRemoveCartItemMutation();

  return { cart, addItem, removeItem };
}
```

## State Management

| State Type | Where |
|-----------|-------|
| Server state | TanStack Query |
| URL state | React Router search params |
| Form state | React Hook Form or local state |
| UI state (local) | `useState` / `useReducer` |
| Shared UI state | React Context (sparingly) |

Avoid global state stores (Redux, Zustand) unless a clear need emerges. TanStack Query + URL state covers most cases.

## Performance

- Use `React.memo` only when profiling shows unnecessary re-renders
- Prefer `useMemo`/`useCallback` for expensive computations, not for every function
- Use `React.lazy` + `Suspense` for route-level code splitting
- Images: use `loading="lazy"` and proper `srcset` for responsive images

## Error Boundaries

- Wrap each route/feature in an error boundary
- Show user-friendly fallback, log error details
- Use `react-error-boundary` package

## Accessibility

- Semantic HTML first (button, nav, main, section)
- ARIA attributes only when HTML semantics are insufficient
- Keyboard navigation for all interactive elements
- Focus management on route changes
