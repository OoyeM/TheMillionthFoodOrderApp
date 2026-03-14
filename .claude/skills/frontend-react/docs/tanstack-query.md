# TanStack Query

## Setup

- Single `QueryClientProvider` at app root
- Default stale time: 5 minutes for catalog data, 30 seconds for order data
- Configure retry and error handling globally

## Query Key Convention

Use a factory pattern for consistent, type-safe keys:

```tsx
// features/products/api/keys.ts
export const productKeys = {
  all: ['products'] as const,
  lists: () => [...productKeys.all, 'list'] as const,
  list: (filters: ProductFilters) => [...productKeys.lists(), filters] as const,
  details: () => [...productKeys.all, 'detail'] as const,
  detail: (id: string) => [...productKeys.details(), id] as const,
};
```

## Query Hooks

One hook per query, co-located with the feature:

```tsx
// features/products/api/useProductsQuery.ts
export function useProductsQuery(filters: ProductFilters) {
  return useQuery({
    queryKey: productKeys.list(filters),
    queryFn: () => api.products.list(filters),
  });
}

// features/products/api/useProductQuery.ts
export function useProductQuery(id: string) {
  return useQuery({
    queryKey: productKeys.detail(id),
    queryFn: () => api.products.get(id),
    enabled: !!id,
  });
}
```

## Mutations

```tsx
export function useCreateOrderMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateOrderRequest) => api.orders.create(data),
    onSuccess: () => {
      // Invalidate related queries
      queryClient.invalidateQueries({ queryKey: orderKeys.lists() });
    },
  });
}
```

## Optimistic Updates

Use for cart operations and other low-risk, high-frequency mutations:

```tsx
export function useAddCartItemMutation() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (item: AddCartItemRequest) => api.cart.addItem(item),
    onMutate: async (newItem) => {
      await queryClient.cancelQueries({ queryKey: cartKeys.current() });
      const previous = queryClient.getQueryData(cartKeys.current());
      queryClient.setQueryData(cartKeys.current(), (old) => ({
        ...old,
        items: [...(old?.items ?? []), newItem],
      }));
      return { previous };
    },
    onError: (_err, _item, context) => {
      queryClient.setQueryData(cartKeys.current(), context?.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: cartKeys.current() });
    },
  });
}
```

## Prefetching

Prefetch on hover/focus for perceived performance:

```tsx
function ProductLink({ productId }: { productId: string }) {
  const queryClient = useQueryClient();

  function handleMouseEnter() {
    queryClient.prefetchQuery({
      queryKey: productKeys.detail(productId),
      queryFn: () => api.products.get(productId),
      staleTime: 60_000,
    });
  }

  return <Link to={`/products/${productId}`} onMouseEnter={handleMouseEnter}>...</Link>;
}
```

## API Client

Centralized API client that handles auth tokens (via BFF cookies) and brand/shop context:

```tsx
// src/api/client.ts
const apiClient = axios.create({
  baseURL: '/api',  // proxied through BFF
  withCredentials: true,
});

// Brand context added automatically
apiClient.interceptors.request.use((config) => {
  const brandSlug = getBrandSlug(); // from URL or context
  config.headers['X-Brand-Slug'] = brandSlug;
  return config;
});
```
