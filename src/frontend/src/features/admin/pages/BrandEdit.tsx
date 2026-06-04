import { useState, useEffect, useCallback } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useConfigureStaffAuth, brandKeys } from '../hooks/useBrands';
import { useResourceForm } from '../forms/useResourceForm';
import { brandsApi } from '../../../api/brands';
import type { UpdateBrandRequest } from '../../../api/brands';
import { brandEditSchema, type BrandEditFormValues } from './schemas/brandEditSchema';
import type { Brand, StaffAuthMethod } from '../../../types/common';
import { labelStyle, inputStyle, secondaryButtonStyle, RequiredMark, FieldError } from '../forms/adminFormStyles';
import { ResourceFormShell } from '../forms/ResourceFormShell';

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

  const resolvedBrandId = brandId ?? '';

  const { t } = useTranslation();

  // ---------------------------------------------------------------------------
  // Main form (name, contactEmail, contactPhone)
  // Note: slug is read-only after creation and not part of the form schema.
  // Staff auth method is a separate imperative concern handled below.
  // ---------------------------------------------------------------------------

  const { form, submit, isSubmitting, isFetching, fetchError, submitError } = useResourceForm<
    Brand,
    BrandEditFormValues,
    UpdateBrandRequest
  >({
    queryKey: brandKeys.detail(resolvedBrandId),
    fetch: () => brandsApi.get(resolvedBrandId),
    update: (payload) => brandsApi.update(resolvedBrandId, payload),
    schema: brandEditSchema,
    defaultValues: { name: '', contactEmail: '', contactPhone: '' },
    toFormValues: (brand) => ({
      name: brand.name,
      contactEmail: brand.contactEmail,
      contactPhone: brand.contactPhone ?? '',
    }),
    toUpdatePayload: (values) => ({
      name: values.name.trim(),
      contactEmail: values.contactEmail.trim(),
      ...(values.contactPhone.trim().length > 0
        ? { contactPhone: values.contactPhone.trim() }
        : {}),
    }),
    invalidate: [brandKeys.all, brandKeys.detail(resolvedBrandId)],
    onSuccess: () => { navigate(`/${brandSlug}/${lang}/admin/brands`); },
  });

  const { register, formState: { errors } } = form;

  // ---------------------------------------------------------------------------
  // Staff auth method — imperative, separate mutation
  // ---------------------------------------------------------------------------

  // displayBrand holds read-only fields (slug, isActive, staffAuthMethod, timestamps)
  // that are not part of the RHF schema. Fetched independently on mount.
  const [displayBrand, setDisplayBrand] = useState<Brand | null>(null);
  const [pendingAuthMethod, setPendingAuthMethod] = useState<StaffAuthMethod | null>(null);
  const configureStaffAuth = useConfigureStaffAuth(resolvedBrandId, displayBrand?.slug ?? '');
  useEffect(() => {
    if (resolvedBrandId) {
      brandsApi.get(resolvedBrandId).then(setDisplayBrand).catch(() => undefined);
    }
  }, [resolvedBrandId]);

  const cancelAuthMethodChange = useCallback(() => {
    setPendingAuthMethod(null);
  }, []);

  function handleAuthMethodChange(method: StaffAuthMethod) {
    if (displayBrand && method !== displayBrand.staffAuthMethod) {
      configureStaffAuth.reset();
      setPendingAuthMethod(method);
    }
  }

  function confirmAuthMethodChange() {
    if (pendingAuthMethod) {
      configureStaffAuth.mutate(pendingAuthMethod, {
        onSuccess: (updated) => {
          setDisplayBrand(updated);
          setPendingAuthMethod(null);
        },
      });
    }
  }

  useEffect(() => {
    if (pendingAuthMethod === null) return;
    function handleKeyDown(e: KeyboardEvent) {
      if (e.key === 'Escape') cancelAuthMethodChange();
    }
    document.addEventListener('keydown', handleKeyDown);
    return () => { document.removeEventListener('keydown', handleKeyDown); };
  }, [pendingAuthMethod, cancelAuthMethodChange]);

  // ---------------------------------------------------------------------------
  // Loading / error states
  // ---------------------------------------------------------------------------

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/brands`);
  }

  // ---------------------------------------------------------------------------
  // Form
  // ---------------------------------------------------------------------------

  return (
    <ResourceFormShell
      isFetching={isFetching}
      fetchError={fetchError}
      resourceName="brand"
      onCancel={handleCancel}
    >
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.75rem', marginBottom: '1.5rem' }}>
        <h1 style={{ fontSize: '1.5rem', fontWeight: 700, margin: 0 }}>
          Edit Brand
        </h1>
        {displayBrand !== null && (
          <span
            style={{
              display: 'inline-block',
              padding: '0.125rem 0.5rem',
              borderRadius: '9999px',
              fontSize: '0.75rem',
              fontWeight: 600,
              background: displayBrand.isActive ? '#d1fae5' : '#fee2e2',
              color: displayBrand.isActive ? '#065f46' : '#991b1b',
            }}
          >
            {displayBrand.isActive ? 'Active' : 'Inactive'}
          </span>
        )}
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

        {/* Slug (read-only after creation) */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="slug">
            Slug
          </label>
          <input
            id="slug"
            type="text"
            value={displayBrand?.slug ?? ''}
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

        {/* Staff Authentication Method — imperative, separate concern */}
        {displayBrand !== null && (
          <fieldset
            style={{
              border: '1px solid #e5e7eb',
              borderRadius: '0.375rem',
              padding: '1rem',
              marginBottom: '1rem',
            }}
          >
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
                  checked={displayBrand.staffAuthMethod === method}
                  onChange={() => { handleAuthMethodChange(method); }}
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
        )}

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
              <h3
                id="staff-auth-dialog-title"
                style={{ fontSize: '1rem', fontWeight: 700, marginBottom: '0.5rem' }}
              >
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
        {displayBrand !== null && (
          <p style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: '1.5rem' }}>
            Created: {new Date(displayBrand.createdAt).toLocaleString()} &mdash; Last updated:{' '}
            {new Date(displayBrand.updatedAt).toLocaleString()}
          </p>
        )}

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
    </ResourceFormShell>
  );
}
