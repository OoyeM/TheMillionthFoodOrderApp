import { useEffect, useRef } from 'react';
import { useForm, type UseFormReturn, type DefaultValues, type FieldValues } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useQuery, useMutation, useQueryClient, type QueryKey } from '@tanstack/react-query';
import type { $ZodType } from 'zod/v4/core';

interface UseResourceFormParams<TResource, TFormValues extends FieldValues, TUpdateInput> {
  /** TanStack Query key used to fetch the resource. */
  queryKey: QueryKey;
  /** Loads the resource from the server (typically `() => api.get(id)`). */
  fetch: () => Promise<TResource>;
  /** Submits the form's update payload (typically `(payload) => api.update(id, payload)`). */
  update: (payload: TUpdateInput) => Promise<TResource>;
  /** zod schema for validation. The schema's inferred type is the form's value type. */
  schema: $ZodType<TFormValues, TFormValues>;
  /** Maps the loaded resource onto the form's initial values. */
  toFormValues: (resource: TResource) => TFormValues;
  /** Maps form values onto the API update payload. Defaults to identity. */
  toUpdatePayload?: (values: TFormValues) => TUpdateInput;
  /** Query keys to invalidate after a successful submit. */
  invalidate?: QueryKey[];
  /** Called after a successful submit (typically navigation). */
  onSuccess?: (updated: TResource) => void;
  /** Default form values used before the resource has loaded. */
  defaultValues: DefaultValues<TFormValues>;
}

interface UseResourceFormResult<TFormValues extends FieldValues> {
  form: UseFormReturn<TFormValues>;
  submit: () => Promise<void>;
  isSubmitting: boolean;
  isFetching: boolean;
  fetchError: unknown;
  submitError: unknown;
}

/**
 * Combines TanStack Query (resource fetch + mutation + cache invalidation)
 * with react-hook-form (form state, validation via zod).
 *
 * The form is initialized with `defaultValues`, then `form.reset(toFormValues(data))`
 * runs exactly once after the first successful fetch. Subsequent re-fetches do NOT
 * reset — that would discard in-progress edits.
 */
export function useResourceForm<TResource, TFormValues extends FieldValues, TUpdateInput = TFormValues>(
  params: UseResourceFormParams<TResource, TFormValues, TUpdateInput>,
): UseResourceFormResult<TFormValues> {
  const {
    queryKey,
    fetch,
    update,
    schema,
    toFormValues,
    toUpdatePayload,
    invalidate = [],
    onSuccess,
    defaultValues,
  } = params;

  const queryClient = useQueryClient();
  const form = useForm<TFormValues>({
    resolver: zodResolver(schema),
    defaultValues,
  });

  const fetchQuery = useQuery<TResource>({
    queryKey,
    queryFn: fetch,
  });

  const hasResetRef = useRef(false);
  useEffect(() => {
    if (fetchQuery.data !== undefined && !hasResetRef.current) {
      form.reset(toFormValues(fetchQuery.data));
      hasResetRef.current = true;
    }
  }, [fetchQuery.data, form, toFormValues]);

  const mutation = useMutation<TResource, unknown, TUpdateInput>({
    mutationFn: update,
    onSuccess: async (updated) => {
      await Promise.all(invalidate.map((key) => queryClient.invalidateQueries({ queryKey: key })));
      onSuccess?.(updated);
    },
  });

  const submit = form.handleSubmit(async (values) => {
    const payload = (toUpdatePayload ? toUpdatePayload(values) : (values as unknown as TUpdateInput));
    // mutateAsync throws on error; suppress so submitError is exposed instead of propagating.
    await mutation.mutateAsync(payload).catch(() => undefined);
  });

  return {
    form,
    submit,
    isSubmitting: mutation.isPending,
    isFetching: fetchQuery.isLoading,
    fetchError: fetchQuery.error,
    submitError: mutation.error,
  };
}
