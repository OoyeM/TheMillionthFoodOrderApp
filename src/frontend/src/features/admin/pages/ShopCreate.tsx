import { useEffect, useRef } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { shopsApi } from '@api/shops';
import type { CreateShopRequest } from '@api/shops';
import { shopCreateSchema, type ShopCreateFormValues } from './schemas/shopCreateSchema';
import { shopKeys } from '../hooks/useShops';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/** Converts a shop name into a URL-safe slug. */
function nameToSlug(name: string): string {
  return name
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function toCreatePayload(values: ShopCreateFormValues): CreateShopRequest {
  return {
    name: values.name.trim(),
    slug: values.slug.trim(),
    address: {
      street: values.address.street.trim(),
      number: values.address.number.trim(),
      city: values.address.city.trim(),
      postalCode: values.address.postalCode.trim(),
      country: values.address.country.trim() || 'BE',
    },
    contactEmail: values.contactEmail.trim(),
    ...(values.contactPhone.trim().length > 0
      ? { contactPhone: values.contactPhone.trim() }
      : {}),
  };
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ShopCreate() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { brandSlug = '', lang = '' } = useParams<{ brandSlug: string; lang: string }>();

  const slugTouched = useRef(false);

  const form = useForm<ShopCreateFormValues>({
    resolver: zodResolver(shopCreateSchema),
    defaultValues: {
      name: '',
      slug: '',
      address: {
        street: '',
        number: '',
        city: '',
        postalCode: '',
        country: 'BE',
      },
      contactEmail: '',
      contactPhone: '',
    },
  });

  const {
    register,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = form;

  // Auto-derive slug from name unless the user has manually edited it
  const watchedName = watch('name');
  useEffect(() => {
    if (!slugTouched.current) {
      setValue('slug', nameToSlug(watchedName), { shouldValidate: false });
    }
  }, [watchedName, setValue]);

  const mutation = useMutation({
    mutationFn: (payload: CreateShopRequest) => shopsApi.create(brandSlug, payload),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: shopKeys.all(brandSlug) });
      navigate('..');
    },
  });

  const onSubmit = handleSubmit((values) => {
    void mutation.mutateAsync(toCreatePayload(values));
  });

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/shops`);
  }

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        Create Shop
      </h1>

      <form onSubmit={(e) => { e.preventDefault(); void onSubmit(); }} noValidate>
        {/* Name */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="name">
            Name <RequiredMark />
          </label>
          <input
            id="name"
            type="text"
            {...register('name')}
            style={inputStyle(!!errors.name)}
            placeholder="e.g. Frietjes? Gent Centrum"
          />
          {errors.name?.message && <FieldError message={errors.name.message} />}
        </div>

        {/* Slug */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="slug">
            Slug <RequiredMark />
          </label>
          <input
            id="slug"
            type="text"
            {...register('slug', {
              onChange: () => {
                slugTouched.current = true;
              },
            })}
            style={inputStyle(!!errors.slug)}
            placeholder="e.g. gent-centrum"
          />
          <p style={{ fontSize: '0.75rem', color: '#6b7280', marginTop: '0.25rem' }}>
            Used in URLs. Lowercase letters, numbers and hyphens only. Cannot be changed after creation.
          </p>
          {errors.slug?.message && <FieldError message={errors.slug.message} />}
        </div>

        {/* Address section */}
        <p style={{ fontWeight: 600, fontSize: '0.875rem', marginBottom: '0.75rem', marginTop: '1.5rem' }}>
          Address
        </p>

        {/* Street + Number on same row */}
        <div style={{ display: 'flex', gap: '0.75rem', marginBottom: '1rem' }}>
          <div style={{ flex: 3 }}>
            <label style={labelStyle} htmlFor="street">
              Street <RequiredMark />
            </label>
            <input
              id="street"
              type="text"
              {...register('address.street')}
              style={inputStyle(!!errors.address?.street)}
              placeholder="e.g. Veldstraat"
            />
            {errors.address?.street?.message && (
              <FieldError message={errors.address.street.message} />
            )}
          </div>
          <div style={{ flex: 1 }}>
            <label style={labelStyle} htmlFor="number">
              Number <RequiredMark />
            </label>
            <input
              id="number"
              type="text"
              {...register('address.number')}
              style={inputStyle(!!errors.address?.number)}
              placeholder="e.g. 12"
            />
            {errors.address?.number?.message && (
              <FieldError message={errors.address.number.message} />
            )}
          </div>
        </div>

        {/* Postal Code + City on same row */}
        <div style={{ display: 'flex', gap: '0.75rem', marginBottom: '1rem' }}>
          <div style={{ flex: 1 }}>
            <label style={labelStyle} htmlFor="postalCode">
              Postal Code <RequiredMark />
            </label>
            <input
              id="postalCode"
              type="text"
              {...register('address.postalCode')}
              style={inputStyle(!!errors.address?.postalCode)}
              placeholder="e.g. 9000"
            />
            {errors.address?.postalCode?.message && (
              <FieldError message={errors.address.postalCode.message} />
            )}
          </div>
          <div style={{ flex: 2 }}>
            <label style={labelStyle} htmlFor="city">
              City <RequiredMark />
            </label>
            <input
              id="city"
              type="text"
              {...register('address.city')}
              style={inputStyle(!!errors.address?.city)}
              placeholder="e.g. Gent"
            />
            {errors.address?.city?.message && (
              <FieldError message={errors.address.city.message} />
            )}
          </div>
        </div>

        {/* Country */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="country">
            Country
          </label>
          <input
            id="country"
            type="text"
            {...register('address.country')}
            style={inputStyle(false)}
            placeholder="BE"
          />
          <p style={{ fontSize: '0.75rem', color: '#6b7280', marginTop: '0.25rem' }}>
            ISO 3166-1 alpha-2 country code (default: BE).
          </p>
        </div>

        {/* Contact Email */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="contactEmail">
            Contact Email <RequiredMark />
          </label>
          <input
            id="contactEmail"
            type="email"
            {...register('contactEmail')}
            style={inputStyle(!!errors.contactEmail)}
            placeholder="e.g. gent@frietjes.be"
          />
          {errors.contactEmail?.message && (
            <FieldError message={errors.contactEmail.message} />
          )}
        </div>

        {/* Contact Phone (optional) */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label style={labelStyle} htmlFor="contactPhone">
            Contact Phone{' '}
            <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
          </label>
          <input
            id="contactPhone"
            type="tel"
            {...register('contactPhone')}
            style={inputStyle(false)}
            placeholder="e.g. +32 9 000 00 00"
          />
        </div>

        {/* API error */}
        {mutation.error != null && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {mutation.error instanceof Error
              ? mutation.error.message
              : 'Failed to create shop. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={isSubmitting || mutation.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: isSubmitting || mutation.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: isSubmitting || mutation.isPending ? 0.6 : 1,
            }}
          >
            {mutation.isPending ? 'Creating…' : 'Create Shop'}
          </button>
          <button
            type="button"
            onClick={handleCancel}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#fff',
              color: '#374151',
              border: '1px solid #d1d5db',
              borderRadius: '0.375rem',
              cursor: 'pointer',
            }}
          >
            Cancel
          </button>
        </div>
      </form>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Small style helpers (avoid repeating inline objects)
// ---------------------------------------------------------------------------

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  fontSize: '0.875rem',
  marginBottom: '0.25rem',
};

function inputStyle(hasError: boolean): React.CSSProperties {
  return {
    width: '100%',
    padding: '0.5rem 0.75rem',
    border: `1px solid ${hasError ? '#dc2626' : '#d1d5db'}`,
    borderRadius: '0.375rem',
    fontSize: '1rem',
    boxSizing: 'border-box',
  };
}

function RequiredMark() {
  return <span style={{ color: '#dc2626' }}>*</span>;
}

function FieldError({ message }: { message: string }) {
  return (
    <p style={{ color: '#dc2626', fontSize: '0.75rem', marginTop: '0.25rem' }}>
      {message}
    </p>
  );
}
