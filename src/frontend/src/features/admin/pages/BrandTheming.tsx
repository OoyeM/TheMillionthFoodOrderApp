import { useState, useEffect, useRef } from 'react';
import { useParams } from 'react-router-dom';
import { useBrandSettings, useUpdateBrandTheming, useUploadBrandLogo } from '../hooks/useBrandSettings';
import { PRESET_FONTS } from '../../../types/common';
import type { BrandColors, BrandTypography } from '../../../types/common';

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

  const { data: settings, isLoading, isError, error } = useBrandSettings(resolvedSlug);
  const updateTheming = useUpdateBrandTheming(resolvedSlug);
  const uploadLogo = useUploadBrandLogo(resolvedSlug);

  // ── Form state ────────────────────────────────────────────────────────────
  const [primaryColor, setPrimaryColor] = useState(DEFAULT_PRIMARY);
  const [secondaryColor, setSecondaryColor] = useState(DEFAULT_SECONDARY);
  const [accentColor, setAccentColor] = useState(DEFAULT_ACCENT);
  const [headingFont, setHeadingFont] = useState(DEFAULT_FONT);
  const [bodyFont, setBodyFont] = useState(DEFAULT_FONT);
  const [customDomain, setCustomDomain] = useState('');
  const [formInitialized, setFormInitialized] = useState(false);
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

  // Populate form when settings data arrives
  useEffect(() => {
    if (settings !== undefined && !formInitialized) {
      setPrimaryColor(settings.colors?.primary ?? DEFAULT_PRIMARY);
      setSecondaryColor(settings.colors?.secondary ?? DEFAULT_SECONDARY);
      setAccentColor(settings.colors?.accent ?? DEFAULT_ACCENT);
      setHeadingFont(settings.typography?.headingFontFamily ?? DEFAULT_FONT);
      setBodyFont(settings.typography?.bodyFontFamily ?? DEFAULT_FONT);
      setCustomDomain(settings.customDomain ?? '');
      setLogoPreview(settings.logoUrl);
      setFormInitialized(true);
    }
  }, [settings, formInitialized]);

  // ── Handlers ──────────────────────────────────────────────────────────────

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();

    const colors: BrandColors = {
      primary: primaryColor,
      secondary: secondaryColor,
      accent: accentColor,
    };

    const typography: BrandTypography = {
      headingFontFamily: headingFont,
      bodyFontFamily: bodyFont,
    };

    updateTheming.mutate({
      colors,
      typography,
      customDomain: customDomain.trim().length > 0 ? customDomain.trim() : null,
    });
  }

  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    const file = e.target.files?.[0];
    if (!file) return;

    // Show a local preview immediately
    const objectUrl = URL.createObjectURL(file);
    setLogoPreview(objectUrl);

    uploadLogo.mutate(file, {
      onSuccess: (result) => {
        // Replace object URL with the persisted server URL
        URL.revokeObjectURL(objectUrl);
        setLogoPreview(result.logoUrl);
      },
      onError: () => {
        URL.revokeObjectURL(objectUrl);
        setLogoPreview(settings?.logoUrl ?? null);
      },
    });

    // Reset file input so the same file can be re-selected after an error
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  }

  // ── Loading / error states ────────────────────────────────────────────────

  if (isLoading) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading theming settings…</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          Failed to load settings:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
      </main>
    );
  }

  // ── Form ──────────────────────────────────────────────────────────────────

  return (
    <main style={{ padding: '1.5rem', maxWidth: '48rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '0.25rem' }}>
        Brand Theming
      </h1>
      <p style={{ color: '#6b7280', fontSize: '0.875rem', marginBottom: '2rem' }}>
        Configure the colors, fonts, and logo that appear on your storefront.
        Changes take effect immediately — no redeploy needed.
      </p>

      {/* ── Logo ─────────────────────────────────────────────────────── */}
      <section style={sectionStyle}>
        <h2 style={sectionHeadingStyle}>Logo</h2>
        <div style={{ display: 'flex', alignItems: 'center', gap: '1.5rem', flexWrap: 'wrap' }}>
          {/* Preview */}
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

      <form onSubmit={handleSubmit} noValidate>
        {/* ── Colors ───────────────────────────────────────────────────── */}
        <section style={sectionStyle}>
          <h2 style={sectionHeadingStyle}>Colors</h2>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(12rem, 1fr))', gap: '1rem' }}>
            <ColorField
              id="primaryColor"
              label="Primary"
              description="Main UI elements (buttons, headings)"
              value={primaryColor}
              onChange={setPrimaryColor}
            />
            <ColorField
              id="secondaryColor"
              label="Secondary"
              description="Supporting UI elements"
              value={secondaryColor}
              onChange={setSecondaryColor}
            />
            <ColorField
              id="accentColor"
              label="Accent"
              description="Highlights, links, call-to-actions"
              value={accentColor}
              onChange={setAccentColor}
            />
          </div>
        </section>

        {/* ── Typography ───────────────────────────────────────────────── */}
        <section style={sectionStyle}>
          <h2 style={sectionHeadingStyle}>Typography</h2>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(16rem, 1fr))', gap: '1rem' }}>
            <FontField
              id="headingFont"
              label="Heading Font"
              description="Applied to h1–h6 elements"
              value={headingFont}
              onChange={setHeadingFont}
            />
            <FontField
              id="bodyFont"
              label="Body Font"
              description="Applied to body text and UI"
              value={bodyFont}
              onChange={setBodyFont}
            />
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
            value={customDomain}
            onChange={(e) => setCustomDomain(e.target.value)}
            style={inputStyle}
          />
        </section>

        {/* ── Live preview panel ────────────────────────────────────────── */}
        <section style={sectionStyle}>
          <h2 style={sectionHeadingStyle}>Live Preview</h2>
          <div
            style={{
              border: '1px solid #e5e7eb',
              borderRadius: '0.5rem',
              overflow: 'hidden',
            }}
          >
            {/* Mock storefront header */}
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
            {/* Mock content area */}
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
        {updateTheming.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {updateTheming.error instanceof Error
              ? updateTheming.error.message
              : 'Failed to save theming. Please try again.'}
          </p>
        )}

        {updateTheming.isSuccess && (
          <p style={{ color: '#059669', marginBottom: '1rem', fontSize: '0.875rem' }}>
            Theming saved successfully.
          </p>
        )}

        <button
          type="submit"
          disabled={updateTheming.isPending}
          style={{
            padding: '0.5rem 1.5rem',
            background: '#111827',
            color: '#fff',
            border: 'none',
            borderRadius: '0.375rem',
            cursor: updateTheming.isPending ? 'not-allowed' : 'pointer',
            fontWeight: 600,
            opacity: updateTheming.isPending ? 0.6 : 1,
          }}
        >
          {updateTheming.isPending ? 'Saving…' : 'Save Theming'}
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
          id={id}
          type="color"
          value={value}
          onChange={(e) => onChange(e.target.value)}
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
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          style={{ ...inputStyle, flex: 1, fontFamily: 'monospace' }}
          pattern="#[0-9a-fA-F]{3,6}"
          maxLength={7}
        />
      </div>
    </div>
  );
}

interface FontFieldProps {
  id: string;
  label: string;
  description: string;
  value: string;
  onChange: (value: string) => void;
}

function FontField({ id, label, description, value, onChange }: FontFieldProps) {
  return (
    <div>
      <label htmlFor={id} style={labelStyle}>
        {label}
      </label>
      <p style={{ fontSize: '0.75rem', color: '#6b7280', marginBottom: '0.375rem' }}>
        {description}
      </p>
      <select
        id={id}
        value={value}
        onChange={(e) => onChange(e.target.value)}
        style={{
          ...inputStyle,
          fontFamily: value === 'System Default' ? 'inherit' : value,
        }}
      >
        {PRESET_FONTS.map((font) => (
          <option
            key={font}
            value={font}
            style={{ fontFamily: font === 'System Default' ? 'inherit' : font }}
          >
            {font}
          </option>
        ))}
      </select>
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
