import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { renderHook, act, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { z } from 'zod';
import { useResourceForm } from '../useResourceForm';
import type { ReactNode } from 'react';

const schema = z.object({
  name: z.string().min(1),
  count: z.number().int().nonnegative(),
});

type FormValues = z.infer<typeof schema>;
interface Resource { id: string; name: string; count: number; }

function makeWrapper(client?: QueryClient) {
  const queryClient = client ?? new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return ({ children }: { children: ReactNode }) => (
    <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
  );
}

describe('useResourceForm', () => {
  beforeEach(() => {
    vi.useRealTimers();
  });

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('seeds form values from fetched resource', async () => {
    const fetch = vi.fn().mockResolvedValue({
      id: 'r1',
      name: 'hello',
      count: 3,
    });

    const { result } = renderHook(
      () => useResourceForm<Resource, FormValues>({
        queryKey: ['resource', 'r1'],
        fetch,
        update: vi.fn(),
        schema,
        toFormValues: (r) => ({ name: r.name, count: r.count }),
        defaultValues: { name: '', count: 0 },
      }),
      { wrapper: makeWrapper() },
    );

    await waitFor(() => {
      expect(result.current.form.getValues()).toEqual({ name: 'hello', count: 3 });
    });
  });

  it('rejects submit when validation fails', async () => {
    const update = vi.fn();
    const { result } = renderHook(
      () => useResourceForm<Resource, FormValues>({
        queryKey: ['r2'],
        fetch: () => Promise.resolve({ id: 'r2', name: 'x', count: 0 }),
        update,
        schema,
        toFormValues: (r) => ({ name: r.name, count: r.count }),
        defaultValues: { name: '', count: 0 },
      }),
      { wrapper: makeWrapper() },
    );

    // Empty out the name field to trigger validation failure
    await act(async () => {
      result.current.form.setValue('name', '');
      await result.current.submit();
    });

    expect(update).not.toHaveBeenCalled();
    // Use getFieldState which works without formState subscription in renderHook
    await waitFor(() => {
      expect(result.current.form.getFieldState('name').error).toBeDefined();
    });
  });

  it('calls update + invalidates + onSuccess on valid submit', async () => {
    const update = vi.fn().mockResolvedValue({ id: 'r3', name: 'new', count: 5 });
    const onSuccess = vi.fn();
    const queryClient = new QueryClient({
      defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
    });
    const invalidateSpy = vi.spyOn(queryClient, 'invalidateQueries');

    const { result } = renderHook(
      () => useResourceForm<Resource, FormValues>({
        queryKey: ['r3'],
        fetch: () => Promise.resolve({ id: 'r3', name: 'old', count: 1 }),
        update,
        schema,
        toFormValues: (r) => ({ name: r.name, count: r.count }),
        invalidate: [['resources', 'list']],
        onSuccess,
        defaultValues: { name: '', count: 0 },
      }),
      { wrapper: makeWrapper(queryClient) },
    );

    await waitFor(() => {
      expect(result.current.form.getValues().name).toBe('old');
    });

    await act(async () => {
      result.current.form.setValue('name', 'new');
      result.current.form.setValue('count', 5);
      await result.current.submit();
    });

    // TanStack Query v5 passes (variables, MutationFunctionContext) to mutationFn.
    // Check only the payload argument (first arg) to avoid coupling to internal context shape.
    expect(update.mock.calls[0]?.[0]).toEqual({ name: 'new', count: 5 });
    expect(invalidateSpy).toHaveBeenCalledWith({ queryKey: ['resources', 'list'] });
    expect(onSuccess).toHaveBeenCalledWith({ id: 'r3', name: 'new', count: 5 });
  });

  it('exposes submitError when mutation rejects', async () => {
    const err = new Error('boom');
    const update = vi.fn().mockRejectedValue(err);

    const { result } = renderHook(
      () => useResourceForm<Resource, FormValues>({
        queryKey: ['r4'],
        fetch: () => Promise.resolve({ id: 'r4', name: 'x', count: 0 }),
        update,
        schema,
        toFormValues: (r) => ({ name: r.name, count: r.count }),
        defaultValues: { name: '', count: 0 },
      }),
      { wrapper: makeWrapper() },
    );

    await waitFor(() => {
      expect(result.current.form.getValues().name).toBe('x');
    });

    await act(async () => {
      result.current.form.setValue('name', 'valid');
      await result.current.submit();
    });

    await waitFor(() => {
      expect(result.current.submitError).toBe(err);
    });
  });
});
