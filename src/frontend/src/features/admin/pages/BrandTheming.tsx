import { useRef, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Controller } from 'react-hook-form';
import { useUploadBrandLogo, brandSettingsKeys } from '../hooks/useBrandSettings';
import { useResourceForm } from '../forms/useResourceForm';
import { brandSettingsApi } from '../../../api/brandSettings';
import type { UpdateBrandThemingRequest, UploadBrandLogoResponse } from '../../../api/brandSettings';
import { PRESET_FONTS } from '../../../types/common';
import type { BrandSettings } from '../../../types/common';
import { brandThemingSchema, type BrandThemingFormValues } from './schemas/brandThemingSchema';

// Default theme values — matches backend defaults
const DEFAULT_PRIMARY = '#111827';
const DEFAULT_SECONDARY = '#6b7280';
const DEFAULT_ACCENT = '#2563eb';
const DEFAULT_FONT = 'System Default';

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function BrandTheming() {
  const { brandSlug } = useParams<{ brandSlug: string; lang: string }>();
  const resolvedSlug = brandSlug ?? '';

  // Logo upload stays imperative — it is a separate resource / mutation.
  const uploadLogo = useUploadBrandLogo(resolvedSlug);
  const [logoPreview, setLogoPreview] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  // Clean up any pending blob URL on unmount
  useEffect(() => {
    return () => {
      if (logoPreview && logoPreview.startsWith('blob:')) {
        URL.revokeObjectURL(logoPreview);
      }
    };
  }, [logoPreview]);

  // ---------------------------------------------------------------------------
  // Main form via useResourceForm (colors, typography, customDomain)
  // ---------------------------------------------------------------------------

  const { form, submit, isSubmitting, isFetching, fetchError, submitError } = useResourceForm<
    BrandSettings,
    BrandThemingFormValues,
    UpdateBrandThemingRequest
  >({
    queryKey: brandSettingsKeys.settings(resolvedSlug),
    fetch: () => brandSettingsApi.get(resolvedSlug),
    update: (payload) => brandSettingsApi.updateTheming(resolvedSlug, payload),
    schema: brandThemingSchema,
    defaultValues: {
      colors: {
        primary: DEFAULT_PRIMARY,
        secondary: DEFAULT_SECONDARY,
        accent: DEFAULT_ACCENT,
      },
      typography: {
        headingFont: DEFAULT_FONT,
        bodyFont: DEFAULT_FONT,
      },
      customDomain: '',
    },
    toFormValues: (settings) => ({
      colors: {
        primary: settings.colors?.primary ?? DEFAULT_PRIMARY,
        secondary: settings.colors?.secondary ?? DEFAULT_SECONDARY,
        accent: settings.colors?.accent ?? DEFAULT_ACCENT,
      },
      typography: {
        headingFont: settings.typography?.headingFontFamily ?? DEFAULT_FONT,
        bodyFont: settings.typography?.bodyFontFamily ?? DEFAULT_FONT,
      },
      customDomain: settings.customDomain ?? '',
    }),
    toUpdatePayload: (values) => ({
      colors: {
        primary: values.colors.primary,
        secondary: values.colors.secondary,
        accent: values.colors.accent,
      },
      typography: {
        headingFontFamily: values.typography.headingFont,
        bodyFontFamily: values.typography.bodyFont,
      },
      customDomain: values.customDomain.trim() || null,
    }),
    invalidate: [
      brandSettingsKeys.settings(resolvedSlug),
      brandSettingsKeys.theme(resolvedSlug),
    ],
    onSuccess: (updated) => {
      // Sync logoPreview with persisted value after a successful save
      setLogoPreview(updated.logoUrl);
    },
  });

  // Seed logoPreview once on first fetch
  useEffect(() => {
    if (logoPreview === null) {
      brandSettingsApi
        .get(resolvedSlug)
        .then((s) => { setLogoPreview(s.logoUrl); })
        .catch(() => undefined);
    }
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [resolvedSlug]);

  const { register, control, watch } = form;
  const headingFont = watch('typography.headingFont');
  const bodyFont = watch('typography.bodyFont');
  const primaryColor = watch('colors.primary');
  const secondaryColor = watch('colors.secondary');
  const accentColor = watch('colors.accent');

  // ---------------------------------------------------------------------------
  // Logo upload handler
  // ---------------------------------------------------------------------------

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    const objectUrl = URL.createObjectURL(file);
    setLogoPreview(objectUrl);

    uploadLogo.mutate(file, {
      onSuccess: (result: UploadBrandLogoResponse) => {
        URL.revokeObjectURL(objectUrl);
        setLogoPreview(result.logoUrl);
      },
      onError: () => {
        URL.revokeObjectURL(objectUrl);
        setLogoPreview(null);
      },
    });

    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }

  // ---------------------------------------------------------------------------
  // Loading / error states
  // ---------------------------------------------------------------------------

  if (isFetching) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading theming settings…</p>
      </main>
    );
  }

  if (fetchError !== null) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          Failed to load settings:{' '}
          {fetchError instanceof Error ? fetchError.message : 'Unknown error'}
        </p>
      </main>
    );
  }

  // ---------------------------------------------------------------------------
  // Form
  // ---------------------------------------------------------------------------

  return (
    <main style={{ padding: '1.5rem', maxWidth: '48rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '0.25rem' }}>
        Brand Theming
      </h1>
      <p style={{ color: '#6b7280', fontSize: '0.875rem', marginBottom: '2rem' }}>
        Configure the colors, fonts, and logo that appear on your storefront.
        Changes take effect immediately — no redeploy needed.
      </p>

      {/* ── Logo (imperative — separate upload mutation) ─────────────── */}
      <section style={sectionStyle}>
        <h2 style={sectionHeadingStyle}>Logo</h2>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1.5rem', flexWrap: 'wrap' }}>
          <div
            style={{
              width: '8rem',
              height: '8rem',
              border: '2px dashed #d1d5db',
              borderRadius: '0.5rem',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              background: '#f9fafb',
              overflow: 'hidden',
              flexShrink: 0,
            }}
          >
            {logoPreview ? (
              <img
                src={logoPreview}
                alt="Brand logo preview"
                style={{ width: '100%', height: '100%', objectFit: 'contain' }}
              />
            ) : (
              <span style={{ fontSize: '0.75rem', color: '#9ca3af', textAlign: 'center', padding: '0.5rem' }}>
                No logo
              </span>
            )}
          </div>

          <div>
            <p style={{ fontSize: '0.875rem', color: '#4b5563', marginBottom: '0.75rem' }}>
              Upload a PNG, JPG, WebP, or SVG file. Maximum 2 MB.
              {uploadLogo.isPending && (
                <span style={{ color: '#2563eb', marginLeft: '0.5rem' }}>Uploading…</span>
              )}
            </p>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png,image/webp,image/svg+xml"
              onChange={handleFileChange}
              disabled={uploadLogo.isPending}
              style={{ fontSize: '0.875rem' }}
            />
            {uploadLogo.isError && (
              <p style={{ color: '#dc2626', fontSize: '0.75rem', marginTop: '0.25rem' }}>
                {uploadLogo.error instanceof Error
                  ? uploadLogo.error.message
                  : 'Failed to upload logo. Please try again.'}
              </p>
            )}
            {uploadLogo.isSuccess && (
              <p style={{ color: '#059669', fontSize: '0.75rem', marginTop: '0.25rem' }}>
                Logo uploaded successfully.
              </p>
            )}
          </div>
        </div>
      </section>

      <form
        onSubmit={(e) => {
          e.preventDefault();
          void submit();
        }}
        noValidate
      >
        {/* ── Colors ───────────────────────────────────────────────────── */}
        <section style={sectionStyle}>
          <h2 style={sectionHeadingStyle}>Colors</h2>
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(12rem, 1fr))',
              gap: '1rem',
            }}
          >
            <Controller
              name="colors.primary"
              control={control}
              render={({ field }) => (
                <ColorField
                  id="primaryColor"
                  label="Primary"
                  description="Main UI elements (buttons, headings)"
                  value={field.value}
                  onChange={field.onChange}
                />
              )}
            />
            <Controller
              name="colors.secondary"
              control={control}
              render={({ field }) => (
                <ColorField
                  id="secondaryColor"
                  label="Secondary"
                  description="Supporting UI elements"
                  value={field.value}
                  onChange={field.onChange}
                />
              )}
            />
            <Controller
              name="colors.accent"
              control={control}
              render={({ field }) => (
                <ColorField
                  id="accentColor"
                  label="Accent"
                  description="Highlights, links, call-to-actions"
                  value={field.value}
                  onChange={field.onChange}
                />
              )}
            />
          </div>
        </section>

        {/* ── Typography ───────────────────────────────────────────────── */}
        <section style={sectionStyle}>
          <h2 style={sectionHeadingStyle}>Typography</h2>
          <div
            style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(16rem, 1fr))',
              gap: '1rem',
            }}
          >
            <div>
              <label htmlFor="headingFont" style={labelStyle}>
                Heading Font
              </label>
              <p style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: '0.375rem' }}>
                Applied to h1–h6 elements
              </p>
              <select
                id="headingFont"
                {...register('typography.headingFont')}
                style={{ ...inputStyle, fontFamily: headingFont === 'System Default' ? 'inherit' : headingFont }}
              >
                {PRESET_FONTS.map((font) => (
                  <option key={font} value={font} style={{ fontFamily: font === 'System Default' ? 'inherit' : font }}>
                    {font}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label htmlFor="bodyFont" style={labelStyle}>
                Body Font
              </label>
              <p style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: '0.375rem' }}>
                Applied to body text and UI
              </p>
              <select
                id="bodyFont"
                {...register('typography.bodyFont')}
                style={{ ...inputStyle, fontFamily: bodyFont === 'System Default' ? 'inherit' : bodyFont }}
              >
                {PRESET_FONTS.map((font) => (
                  <option key={font} value={font} style={{ fontFamily: font === 'System Default' ? 'inherit' : font }}>
                    {font}
                  </option>
                ))}
              </select>
            </div>
          </div>

          {/* Live font preview */}
          <div
            style={{
              marginTop: '1rem',
              padding: '1rem',
              border: '1px solid #e5e7eb',
              borderRadius: '0.375rem',
              background: '#f9fafb',
            }}
          >
            <p style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: '0.5rem' }}>Preview</p>
            <p
              style={{
                fontFamily: headingFont === 'System Default' ? 'inherit' : headingFont,
                fontSize: '1.25rem',
                fontWeight: 700,
                margin: '0 0 0.25rem',
              }}
            >
              The quick brown fox
            </p>
            <p
              style={{
                fontFamily: bodyFont === 'System Default' ? 'inherit' : bodyFont,
                fontSize: '0.875rem',
                color: '#4b5563',
                margin: 0,
              }}
            >
              The quick brown fox jumps over the lazy dog. 1234567890
            </p>
          </div>
        </section>

        {/* ── Custom Domain ─────────────────────────────────────────────── */}
        <section style={sectionStyle}>
          <h2 style={sectionHeadingStyle}>Custom Domain</h2>
          <p style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.75rem' }}>
            Enter a custom domain for your storefront (e.g. <code>order.frietjes.be</code>).
            DNS routing must be configured separately.
          </p>
          <input
            id="customDomain"
            type="text"
            placeholder="order.yourbrand.com"
            {...register('customDomain')}
            style={inputStyle}
          />
        </section>

        {/* ── Live preview panel ────────────────────────────────────────── */}
        <section style={sectionStyle}>
          <h2 style={sectionHeadingStyle}>Live Preview</h2>
          <div style={{ border: '1px solid #e5e7eb', borderRadius: '0.5rem', overflow: 'hidden' }}>
            <div
              style={{
                background: primaryColor,
                padding: '1rem 1.5rem',
                display: 'flex',
                alignItems: 'center',
                gap: '1rem',
              }}
            >
              {logoPreview ? (
                <img
                  src={logoPreview}
                  alt="Brand logo"
                  style={{ height: '2rem', objectFit: 'contain' }}
                />
              ) : (
                <span
                  style={{
                    fontFamily: headingFont === 'System Default' ? 'inherit' : headingFont,
                    fontWeight: 700,
                    fontSize: '1.25rem',
                    color: '#fff',
                  }}
                >
                  Your Brand
                </span>
              )}
            </div>
            <div style={{ padding: '1.5rem', background: '#fff' }}>
              <h3
                style={{
                  fontFamily: headingFont === 'System Default' ? 'inherit' : headingFont,
                  color: primaryColor,
                  marginBottom: '0.5rem',
                  fontSize: '1.125rem',
                  fontWeight: 700,
                }}
              >
                Welcome to our menu
              </h3>
              <p
                style={{
                  fontFamily: bodyFont === 'System Default' ? 'inherit' : bodyFont,
                  color: secondaryColor,
                  fontSize: '0.875rem',
                  marginBottom: '1rem',
                }}
              >
                Discover our fresh and delicious offerings.
              </p>
              <button
                type="button"
                style={{
                  background: accentColor,
                  color: '#fff',
                  border: 'none',
                  borderRadius: '0.375rem',
                  padding: '0.5rem 1rem',
                  fontFamily: bodyFont === 'System Default' ? 'inherit' : bodyFont,
                  cursor: 'default',
                }}
              >
                Order now
              </button>
            </div>
          </div>
        </section>

        {/* ── Form actions ─────────────────────────────────────────────── */}
        {submitError != null && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {submitError instanceof Error
              ? submitError.message
              : 'Failed to save theming. Please try again.'}
          </p>
        )}

        <button
          type="submit"
          disabled={isSubmitting}
          style={{
            padding: '0.5rem 1.5rem',
            background: '#111827',
            color: '#fff',
            border: 'none',
            borderRadius: '0.375rem',
            cursor: isSubmitting ? 'not-allowed' : 'pointer',
            fontWeight: 600,
            opacity: isSubmitting ? 0.6 : 1,
          }}
        >
          {isSubmitting ? 'Saving…' : 'Save Theming'}
        </button>
      </form>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Sub-components
// ---------------------------------------------------------------------------

interface ColorFieldProps {
  id: string;
  label: string;
  description: string;
  value: string;
  onChange: (value: string) => void;
}

function ColorField({ id, label, description, value, onChange }: ColorFieldProps) {
  return (
    <div>
      <label htmlFor={id} style={labelStyle}>
        {label}
      </label>
      <p style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: '0.375rem' }}>
        {description}
      </p>
      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
        <input
          type="color"
          value={value}
          onChange={(e) => { onChange(e.target.value); }}
          style={{
            width: '2.5rem',
            height: '2.5rem',
            padding: '0.125rem',
            border: '1px solid #d1d5db',
            borderRadius: '0.375rem',
            cursor: 'pointer',
          }}
        />
        <input
          id={id}
          type="text"
          value={value}
          onChange={(e) => { onChange(e.target.value); }}
          style={{ ...inputStyle, flex: 1, fontFamily: 'monospace' }}
          pattern="#[0-9a-fA-F]{3,6}"
          maxLength={7}
        />
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Style helpers
// ---------------------------------------------------------------------------

const sectionStyle: React.CSSProperties = {
  marginBottom: '2rem',
};

const sectionHeadingStyle: React.CSSProperties = {
  fontSize: '1rem',
  fontWeight: 700,
  marginBottom: '0.75rem',
  paddingBottom: '0.5rem',
  borderBottom: '1px solid #e5e7eb',
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  fontSize: '0.875rem',
  marginBottom: '0.125rem',
};

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '0.5rem 0.75rem',
  border: '1px solid #d1d5db',
  borderRadius: '0.375rem',
  fontSize: '0.875rem',
  boxSizing: 'border-box',
};
