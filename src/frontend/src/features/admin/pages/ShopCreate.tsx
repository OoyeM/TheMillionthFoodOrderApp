import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useCreateShop } from '../hooks/useShops';

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

const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface FormErrors {
  name?: string;
  slug?: string;
  street?: string;
  number?: string;
  city?: string;
  postalCode?: string;
  contactEmail?: string;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ShopCreate() {
  const navigate = useNavigate();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const createShop = useCreateShop(resolvedBrandSlug);

  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [slugTouched, setSlugTouched] = useState(false);
  const [street, setStreet] = useState('');
  const [number, setNumber] = useState('');
  const [city, setCity] = useState('');
  const [postalCode, setPostalCode] = useState('');
  const [country, setCountry] = useState('BE');
  const [contactEmail, setContactEmail] = useState('');
  const [contactPhone, setContactPhone] = useState('');
  const [errors, setErrors] = useState<FormErrors>({});

  // Auto-derive slug from name unless the user has manually edited it
  function handleNameChange(value: string) {
    setName(value);
    if (!slugTouched) {
      setSlug(nameToSlug(value));
    }
  }

  function handleSlugChange(value: string) {
    setSlugTouched(true);
    setSlug(value);
  }

  function validate(): FormErrors {
    const next: FormErrors = {};

    if (name.trim().length === 0) {
      next.name = 'Name is required.';
    }

    if (slug.trim().length === 0) {
      next.slug = 'Slug is required.';
    } else if (!SLUG_PATTERN.test(slug)) {
      next.slug =
        'Slug must be lowercase letters, numbers and hyphens only (e.g. my-shop).';
    }

    if (street.trim().length === 0) {
      next.street = 'Street is required.';
    }

    if (number.trim().length === 0) {
      next.number = 'House number is required.';
    }

    if (city.trim().length === 0) {
      next.city = 'City is required.';
    }

    if (postalCode.trim().length === 0) {
      next.postalCode = 'Postal code is required.';
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
    createShop.mutate(
      {
        name: name.trim(),
        slug: slug.trim(),
        address: {
          street: street.trim(),
          number: number.trim(),
          city: city.trim(),
          postalCode: postalCode.trim(),
          country: country.trim() || 'BE',
        },
        contactEmail: contactEmail.trim(),
        ...(contactPhone.trim().length > 0 ? { contactPhone: contactPhone.trim() } : {}),
      },
      {
        onSuccess: () => {
          navigate(`/${brandSlug}/${lang}/admin/shops`);
        },
      },
    );
  }

  function handleCancel() {
    navigate(`/${brandSlug}/${lang}/admin/shops`);
  }

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        Create Shop
      </h1>

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
            onChange={(e) => handleNameChange(e.target.value)}
            style={inputStyle(!!errors.name)}
            placeholder="e.g. Frietjes? Gent Centrum"
          />
          {errors.name && <FieldError message={errors.name} />}
        </div>

        {/* Slug */}
        <div style={{ marginBottom: '1rem' }}>
          <label style={labelStyle} htmlFor="slug">
            Slug <RequiredMark />
          </label>
          <input
            id="slug"
            type="text"
            value={slug}
            onChange={(e) => handleSlugChange(e.target.value)}
            style={inputStyle(!!errors.slug)}
            placeholder="e.g. gent-centrum"
          />
          <p style={{ fontSize: '0.75rem', color: '#6b7280', marginTop: '0.25rem' }}>
            Used in URLs. Lowercase letters, numbers and hyphens only. Cannot be changed after creation.
          </p>
          {errors.slug && <FieldError message={errors.slug} />}
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
              value={street}
              onChange={(e) => setStreet(e.target.value)}
              style={inputStyle(!!errors.street)}
              placeholder="e.g. Veldstraat"
            />
            {errors.street && <FieldError message={errors.street} />}
          </div>
          <div style={{ flex: 1 }}>
            <label style={labelStyle} htmlFor="number">
              Number <RequiredMark />
            </label>
            <input
              id="number"
              type="text"
              value={number}
              onChange={(e) => setNumber(e.target.value)}
              style={inputStyle(!!errors.number)}
              placeholder="e.g. 12"
            />
            {errors.number && <FieldError message={errors.number} />}
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
              value={postalCode}
              onChange={(e) => setPostalCode(e.target.value)}
              style={inputStyle(!!errors.postalCode)}
              placeholder="e.g. 9000"
            />
            {errors.postalCode && <FieldError message={errors.postalCode} />}
          </div>
          <div style={{ flex: 2 }}>
            <label style={labelStyle} htmlFor="city">
              City <RequiredMark />
            </label>
            <input
              id="city"
              type="text"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              style={inputStyle(!!errors.city)}
              placeholder="e.g. Gent"
            />
            {errors.city && <FieldError message={errors.city} />}
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
            value={country}
            onChange={(e) => setCountry(e.target.value)}
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
            value={contactEmail}
            onChange={(e) => setContactEmail(e.target.value)}
            style={inputStyle(!!errors.contactEmail)}
            placeholder="e.g. gent@frietjes.be"
          />
          {errors.contactEmail && <FieldError message={errors.contactEmail} />}
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
            value={contactPhone}
            onChange={(e) => setContactPhone(e.target.value)}
            style={inputStyle(false)}
            placeholder="e.g. +32 9 000 00 00"
          />
        </div>

        {/* API error */}
        {createShop.isError && (
          <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
            {createShop.error instanceof Error
              ? createShop.error.message
              : 'Failed to create shop. Please try again.'}
          </p>
        )}

        <div style={{ display: 'flex', gap: '0.75rem' }}>
          <button
            type="submit"
            disabled={createShop.isPending}
            style={{
              padding: '0.5rem 1.25rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: createShop.isPending ? 'not-allowed' : 'pointer',
              fontWeight: 600,
              opacity: createShop.isPending ? 0.6 : 1,
            }}
          >
            {createShop.isPending ? 'Creating…' : 'Create Shop'}
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
