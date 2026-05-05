import { useNavigate, useParams } from 'react-router-dom';
import { shopKeys } from '../hooks/useShops';
import { useResourceForm } from '../forms/useResourceForm';
import { shopsApi } from '../../../api/shops';
import type { UpdateShopRequest } from '../../../api/shops';
import type { Shop } from '../../../types/common';
import { shopEditSchema, type ShopEditFormValues } from './schemas/shopEditSchema';

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ShopEdit() {
  const navigate = useNavigate();
  const { brandSlug, lang, shopId } = useParams<{
    brandSlug: string;
    lang: string;
    shopId: string;
  }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedShopId = shopId ?? '';

  // ---------------------------------------------------------------------------
  // Main form via useResourceForm
  // ---------------------------------------------------------------------------

  const { form, submit, isSubmitting, isFetching, fetchError, submitError } = useResourceForm<
    Shop,
    ShopEditFormValues,
    UpdateShopRequest
  >({
    queryKey: shopKeys.detail(resolvedBrandSlug, resolvedShopId),
    fetch: () => shopsApi.get(resolvedBrandSlug, resolvedShopId),
    update: (payload) => shopsApi.update(resolvedBrandSlug, resolvedShopId, payload),
    schema: shopEditSchema,
    defaultValues: {
      name: '',
      address: { street: '', number: '', city: '', postalCode: '', country: 'BE' },
      contactEmail: '',
      contactPhone: '',
    },
    toFormValues: (shop) => ({
      name: shop.name,
      address: {
        street: shop.address.street,
        number: shop.address.number,
        city: shop.address.city,
        postalCode: shop.address.postalCode,
        country: shop.address.country,
      },
      contactEmail: shop.contactEmail,
      contactPhone: shop.contactPhone ?? '',
    }),
    toUpdatePayload: (values) => ({
      name: values.name.trim(),
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
    }),
    invalidate: [shopKeys.all(resolvedBrandSlug), shopKeys.detail(resolvedBrandSlug, resolvedShopId)],
    onSuccess: () => navigate(`/${brandSlug}/${lang}/admin/shops`),
  });

  const { register, formState: { errors } } = form;

  // ---------------------------------------------------------------------------
  // Handlers
  // ---------------------------------------------------------------------------

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/shops`);
  }

  // ---------------------------------------------------------------------------
  // Loading / error states
  // ---------------------------------------------------------------------------

  if (isFetching) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading shop…</p>
      </main>
    );
  }

  if (fetchError !== null) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          Failed to load shop:{' '}
          {fetchError instanceof Error ? fetchError.message : 'Unknown error'}
        </p>
        <button onClick={handleCancel} style={secondaryButtonStyle}>
          Back to list
        </button>
      </main>
    );
  }

  // ---------------------------------------------------------------------------
  // Form
  // ---------------------------------------------------------------------------

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.5rem', fontWeight: 700, margin: 0 }}>
          Edit Shop
        </h1>
      </div>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          void submit();
        }}
        noValidate
      >
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
          />
          {errors.name?.message && <FieldError message={errors.name.message} />}
        </div>

        {/* Slug (read-only — displayed from fetched data via defaultValues + reset) */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="slug">
            Slug
          </label>
          <input
            id="slug"
            type="text"
            readOnly
            disabled
            style={{
              ...inputStyle(false),
              background: '#f9fafb',
              color: '#6b7280',
              cursor: 'not-allowed',
            }}
          />
          <p style={{ fontSize: '0.75rem', color: '#6b7280', marginTop: '0.25rem' }}>
            Slug cannot be changed after creation.
          </p>
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
          />
          {errors.contactEmail?.message && <FieldError message={errors.contactEmail.message} />}
        </div>

        {/* Contact Phone (optional) */}
        <div style={{ marginBottom: '0.5rem' }}>
          <label style={labelStyle} htmlFor="contactPhone">
            Contact Phone{' '}
            <span style={{ color: '#9ca3af', fontWeight: 400 }}>(optional)</span>
          </label>
          <input
            id="contactPhone"
            type="tel"
            {...register('contactPhone')}
            style={inputStyle(false)}
          />
        </div>

        {/* Quick links to shop config pages */}
        <div style={{ marginBottom: '1.5rem', marginTop: '1rem', display: 'flex', gap: '0.75rem', flexWrap: 'wrap' }}>
          <button
            type="button"
            onClick={() =>
              navigate(`/${brandSlug}/${lang}/admin/shops/${resolvedShopId}/opening-hours`)
            }
            style={{
              padding: '0.5rem 1.25rem',
              background: '#fff',
              color: '#374151',
              border: '1px solid #d1d5db',
              borderRadius: '0.375rem',
              cursor: 'pointer',
              fontSize: '0.875rem',
            }}
          >
            Manage Opening Hours
          </button>
          <button
            type="button"
            onClick={() =>
              navigate(`/${brandSlug}/${lang}/admin/shops/${resolvedShopId}/order-lifecycle`)
            }
            style={{
              padding: '0.5rem 1.25rem',
              background: '#fff',
              color: '#374151',
              border: '1px solid #d1d5db',
              borderRadius: '0.375rem',
              cursor: 'pointer',
              fontSize: '0.875rem',
            }}
          >
            Manage Order Lifecycle
          </button>
        </div>

        {/* API error */}
        {submitError != null && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {submitError instanceof Error
              ? submitError.message
              : 'Failed to save changes. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={isSubmitting}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: isSubmitting ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: isSubmitting ? 0.6 : 1,
            }}
          >
            {isSubmitting ? 'Saving…' : 'Save Changes'}
          </button>
          <button type="button" onClick={handleCancel} style={secondaryButtonStyle}>
            Cancel
          </button>
        </div>
      </form>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Small style helpers
// ---------------------------------------------------------------------------

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  fontSize: '0.875rem',
  marginBottom: '0.25rem',
};

const secondaryButtonStyle: React.CSSProperties = {
  padding: '0.5rem 1.25rem',
  background: '#fff',
  color: '#374151',
  border: '1px solid #d1d5db',
  borderRadius: '0.375rem',
  cursor: 'pointer',
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
