/* eslint-disable react-refresh/only-export-components --
   Intentional shared module: exports admin-form style constants alongside two tiny
   presentational helpers (RequiredMark, FieldError). Splitting them would churn every
   importer for a HMR-only hint with no correctness benefit. */
import type React from 'react';

// ---------------------------------------------------------------------------
// Shared style constants and helpers for admin form pages
// ---------------------------------------------------------------------------

export const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  fontSize: '0.875rem',
  marginBottom: '0.25rem',
};

export const secondaryButtonStyle: React.CSSProperties = {
  padding: '0.5rem 1.25rem',
  background: '#fff',
  color: '#374151',
  border: '1px solid #d1d5db',
  borderRadius: '0.375rem',
  cursor: 'pointer',
};

export function primaryButtonStyle(disabled: boolean): React.CSSProperties {
  return {
    padding: '0.5rem 1.25rem',
    background: '#111827',
    color: '#fff',
    border: 'none',
    borderRadius: '0.375rem',
    cursor: disabled ? 'not-allowed' : 'pointer',
    fontWeight: 600,
    opacity: disabled ? 0.6 : 1,
  };
}

export function inputStyle(hasError: boolean): React.CSSProperties {
  return {
    width: '100%',
    padding: '0.5rem 0.75rem',
    border: `1px solid ${hasError ? '#dc2626' : '#d1d5db'}`,
    borderRadius: '0.375rem',
    fontSize: '1rem',
    boxSizing: 'border-box',
  };
}

export function RequiredMark(): React.JSX.Element {
  return <span style={{ color: '#dc2626' }}>*</span>;
}

export function FieldError({ message }: { message: string }): React.JSX.Element {
  return (
    <p style={{ color: '#dc2626', fontSize: '0.75rem', marginTop: '0.25rem' }}>
      {message}
    </p>
  );
}

/**
 * Form-level (submission) error banner. Renders nothing when `error` is null/undefined,
 * the message when it is an Error, or `fallback` otherwise.
 */
export function FormError({
  error,
  fallback,
}: {
  error: unknown;
  fallback: string;
}): React.JSX.Element | null {
  if (error == null) return null;
  return (
    <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
      {error instanceof Error ? error.message : fallback}
    </p>
  );
}
