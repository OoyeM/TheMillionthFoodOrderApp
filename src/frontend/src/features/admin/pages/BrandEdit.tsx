import { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useBrand, useUpdateBrand, useConfigureStaffAuth } from '../hooks/useBrands';
import type { StaffAuthMethod } from '../../../types/common';

// ---------------------------------------------------------------------------
// Validation helpers
// ---------------------------------------------------------------------------

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface FormErrors {
  name?: string;
  contactEmail?: string;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function BrandEdit() {
  const navigate = useNavigate();
  const { brandSlug, lang, brandId } = useParams<{
    brandSlug: string;
    lang: string;
    brandId: string;
  }>();

  // brandId is guaranteed by the route definition; guard for type safety
  const resolvedBrandId = brandId ?? '';

  const { t } = useTranslation();
  const { data: brand, isLoading, isError, error } = useBrand(resolvedBrandId);
  const updateBrand = useUpdateBrand(resolvedBrandId);
  const configureStaffAuth = useConfigureStaffAuth(resolvedBrandId, brand?.slug ?? '');

  const [name, setName] = useState('');
  const [contactEmail, setContactEmail] = useState('');
  const [contactPhone, setContactPhone] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});
  const [formInitialized, setFormInitialized] = useState(false);
  const [pendingAuthMethod, setPendingAuthMethod] = useState<StaffAuthMethod | null>(null);

  // Populate form when brand data arrives
  useEffect(() => {
    if (brand !== undefined && !formInitialized) {
      setName(brand.name);
      setContactEmail(brand.contactEmail);
      setContactPhone(brand.contactPhone ?? '');
      setFormInitialized(true);
    }
  }, [brand, formInitialized]);

  function validate(): FormErrors {
    const next: FormErrors = {};

    if (name.trim().length === 0) {
      next.name = 'Name is required.';
    }

    if (contactEmail.trim().length === 0) {
      next.contactEmail = 'Contact email is required.';
    } else if (!EMAIL_PATTERN.test(contactEmail)) {
      next.contactEmail = 'Enter a valid email address.';
    }

    return next;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const validationErrors = validate();
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setErrors({});
    updateBrand.mutate(
      {
        name: name.trim(),
        contactEmail: contactEmail.trim(),
        ...(contactPhone.trim().length > 0 ? { contactPhone: contactPhone.trim() } : {}),
      },
      {
        onSuccess: () => {
          navigate(`/${brandSlug}/${lang}/admin/brands`);
        },
      },
    );
  }

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/brands`);
  }

  function handleAuthMethodChange(method: StaffAuthMethod) {
    if (brand && method !== brand.staffAuthMethod) {
      configureStaffAuth.reset();
      setPendingAuthMethod(method);
    }
  }

  function confirmAuthMethodChange() {
    if (pendingAuthMethod) {
      configureStaffAuth.mutate(pendingAuthMethod, {
        onSuccess: () => setPendingAuthMethod(null),
      });
    }
  }

  const cancelAuthMethodChange = useCallback(() => {
    setPendingAuthMethod(null);
  }, []);

  // Close confirmation dialog on Escape key
  useEffect(() => {
    if (pendingAuthMethod === null) return;
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') cancelAuthMethodChange();
    }
    document.addEventListener('keydown', handleKeyDown);
    return () => document.removeEventListener('keydown', handleKeyDown);
  }, [pendingAuthMethod, cancelAuthMethodChange]);

  // ---------------------------------------------------------------------------
  // Loading / error states
  // ---------------------------------------------------------------------------

  if (isLoading) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading brand…</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          Failed to load brand:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
        <button onClick={handleCancel} style={secondaryButtonStyle}>
          Back to list
        </button>
      </main>
    );
  }

  if (brand === undefined) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Brand not found.</p>
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
          Edit Brand
        </h1>
        <span
          style={{
            display: 'inline-block',
            padding: '0.125rem 0.5rem',
            borderRadius: '9999px',
            fontSize: '0.75rem',
            fontWeight: 600,
            background: brand.isActive ? '#d1fae5' : '#fee2e2',
            color: brand.isActive ? '#065f46' : '#991b1b',
          }}
        >
          {brand.isActive ? 'Active' : 'Inactive'}
        </span>
      </div>

      <form onSubmit={handleSubmit} noValidate>
        {/* Name */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="name">
            Name <RequiredMark />
          </label>
          <input
            id="name"
            type="text"
            value={name}
            onChange={(e) => setName(e.target.value)}
            style={inputStyle(!!errors.name)}
          />
          {errors.name && <FieldError message={errors.name} />}
        </div>

        {/* Slug (read-only after creation) */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="slug">
            Slug
          </label>
          <input
            id="slug"
            type="text"
            value={brand.slug}
            readOnly
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

        {/* Contact Email */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="contactEmail">
            Contact Email <RequiredMark />
          </label>
          <input
            id="contactEmail"
            type="email"
            value={contactEmail}
            onChange={(e) => setContactEmail(e.target.value)}
            style={inputStyle(!!errors.contactEmail)}
          />
          {errors.contactEmail && <FieldError message={errors.contactEmail} />}
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
            value={contactPhone}
            onChange={(e) => setContactPhone(e.target.value)}
            style={inputStyle(false)}
          />
        </div>

        {/* Staff Authentication Method */}
        <fieldset style={{ border: '1px solid #e5e7eb', borderRadius: '0.375rem', padding: '1rem', marginBottom: '1rem' }}>
          <legend style={{ fontWeight: 600, fontSize: '0.875rem', padding: '0 0.25rem' }}>
            {t('admin.staffAuth.title')}
          </legend>
          <p style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: '0.75rem' }}>
            {t('admin.staffAuth.description')}
          </p>
          {(['EmailPassword', 'GoogleSso', 'MicrosoftSso'] as const).map((method) => (
            <label
              key={method}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '0.5rem',
                padding: '0.375rem 0',
                cursor: configureStaffAuth.isPending ? 'not-allowed' : 'pointer',
              }}
            >
              <input
                type="radio"
                name="staffAuthMethod"
                value={method}
                checked={brand.staffAuthMethod === method}
                onChange={() => handleAuthMethodChange(method)}
                disabled={configureStaffAuth.isPending}
              />
              <span style={{ fontSize: '0.875rem' }}>
                {t(`admin.staffAuth.methods.${method}`)}
              </span>
            </label>
          ))}
          {configureStaffAuth.isError && (
            <p style={{ color: '#dc2626', fontSize: '0.75rem', marginTop: '0.5rem' }}>
              {configureStaffAuth.error instanceof Error
                ? configureStaffAuth.error.message
                : t('admin.staffAuth.error')}
            </p>
          )}
        </fieldset>

        {/* Confirmation dialog for auth method change */}
        {pendingAuthMethod !== null && (
          <div
            role="dialog"
            aria-modal="true"
            aria-labelledby="staff-auth-dialog-title"
            style={{
              position: 'fixed',
              inset: 0,
              background: 'rgba(0, 0, 0, 0.5)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              zIndex: 50,
            }}
          >
            <div
              style={{
                background: '#fff',
                borderRadius: '0.5rem',
                padding: '1.5rem',
                maxWidth: '28rem',
                width: '100%',
                boxShadow: '0 20px 25px -5px rgba(0, 0, 0, 0.1)',
              }}
            >
              <h3 id="staff-auth-dialog-title" style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '0.5rem' }}>
                {t('admin.staffAuth.confirmTitle')}
              </h3>
              <p style={{ fontSize: '0.875rem', color: '#4b5563', marginBottom: '1.25rem' }}>
                {t('admin.staffAuth.changeWarning')}
              </p>
              <div style={{ display: 'flex', gap: '0.75rem', justifyContent: 'flex-end' }}>
                <button
                  type="button"
                  onClick={cancelAuthMethodChange}
                  style={secondaryButtonStyle}
                  disabled={configureStaffAuth.isPending}
                >
                  {t('admin.staffAuth.cancel')}
                </button>
                <button
                  type="button"
                  onClick={confirmAuthMethodChange}
                  disabled={configureStaffAuth.isPending}
                  style={{
                    padding: '0.5rem 1.25rem',
                    background: '#dc2626',
                    color: '#fff',
                    border: 'none',
                    borderRadius: '0.375rem',
                    cursor: configureStaffAuth.isPending ? 'not-allowed' : 'pointer',
                    fontWeight: 600,
                    opacity: configureStaffAuth.isPending ? 0.6 : 1,
                  }}
                >
                  {configureStaffAuth.isPending
                    ? t('admin.staffAuth.saving')
                    : t('admin.staffAuth.confirm')}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Metadata */}
        <p style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: '1.5rem' }}>
          Created: {new Date(brand.createdAt).toLocaleString()} &mdash; Last updated:{' '}
          {new Date(brand.updatedAt).toLocaleString()}
        </p>

        {/* API error */}
        {updateBrand.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {updateBrand.error instanceof Error
              ? updateBrand.error.message
              : 'Failed to save changes. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={updateBrand.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: updateBrand.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: updateBrand.isPending ? 0.6 : 1,
            }}
          >
            {updateBrand.isPending ? 'Saving…' : 'Save Changes'}
          </button>
          <button
            type="button"
            onClick={handleCancel}
            style={secondaryButtonStyle}
          >
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
