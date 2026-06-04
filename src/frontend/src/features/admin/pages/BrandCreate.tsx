import { useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useCreateBrand } from '../hooks/useBrands';
import { labelStyle, inputStyle, RequiredMark, FieldError, FormError } from '../forms/adminFormStyles';
import { FormActions } from '../forms/FormActions';
import { nameToSlug } from '../forms/slug';

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

const SLUG_PATTERN = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;
const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface FormErrors {
  name?: string;
  slug?: string;
  contactEmail?: string;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function BrandCreate() {
  const navigate = useNavigate();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const createBrand = useCreateBrand();

  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [slugTouched, setSlugTouched] = useState(false);
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
        'Slug must be lowercase letters, numbers and hyphens only (e.g. my-brand).';
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
    createBrand.mutate(
      {
        name: name.trim(),
        slug: slug.trim(),
        contactEmail: contactEmail.trim(),
        ...(contactPhone.trim().length > 0 ? { contactPhone: contactPhone.trim() } : {}),
      },
      {
        onSuccess: () => {
          navigate(`/${String(brandSlug)}/${String(lang)}/admin/brands`);
        },
      },
    );
  }

  function handleCancel() {
    navigate(`/${String(brandSlug)}/${String(lang)}/admin/brands`);
  }

  return (
    <main style={{ padding: '1.5rem', maxWidth: '40rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '1.5rem' }}>
        Create Brand
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
            onChange={(e) => { handleNameChange(e.target.value); }}
            style={inputStyle(!!errors.name)}
            placeholder="e.g. Frietjes?"
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
            onChange={(e) => { handleSlugChange(e.target.value); }}
            style={inputStyle(!!errors.slug)}
            placeholder="e.g. frietjes"
          />
          <p style={{ fontSize: '0.75rem', color: '#6b7280', marginTop: '0.25rem' }}>
            Used in URLs. Lowercase letters, numbers and hyphens only. Cannot be changed after creation.
          </p>
          {errors.slug && <FieldError message={errors.slug} />}
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
            onChange={(e) => { setContactEmail(e.target.value); }}
            style={inputStyle(!!errors.contactEmail)}
            placeholder="e.g. hello@frietjes.be"
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
            onChange={(e) => { setContactPhone(e.target.value); }}
            style={inputStyle(false)}
            placeholder="e.g. +32 9 000 00 00"
          />
        </div>

        {/* API error */}
        <FormError error={createBrand.error} fallback="Failed to create brand. Please try again." />

        <FormActions
          isPending={createBrand.isPending}
          onCancel={handleCancel}
          submitLabel="Create Brand"
          pendingLabel="Creating…"
        />
      </form>
    </main>
  );
}

