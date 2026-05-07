import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useOpeningHours, useSetOpeningHours } from '../hooks/useOpeningHours';
import type { TimeBlockRequest } from '../../../types/common';

// ---------------------------------------------------------------------------
// Day ordering: European convention — Monday first (1), Sunday last (0).
// .NET DayOfWeek: 0=Sunday, 1=Monday, ..., 6=Saturday.
// ---------------------------------------------------------------------------

const DAYS_ORDER = [1, 2, 3, 4, 5, 6, 0] as const;

type DayOfWeek = (typeof DAYS_ORDER)[number];

const DAY_KEY_MAP: Record<DayOfWeek, string> = {
  1: 'admin.shops.openingHours.monday',
  2: 'admin.shops.openingHours.tuesday',
  3: 'admin.shops.openingHours.wednesday',
  4: 'admin.shops.openingHours.thursday',
  5: 'admin.shops.openingHours.friday',
  6: 'admin.shops.openingHours.saturday',
  0: 'admin.shops.openingHours.sunday',
};

// ---------------------------------------------------------------------------
// Local state shape: one entry per time block on the form
// ---------------------------------------------------------------------------

interface LocalTimeBlock {
  /** Temporary client-side id for React key — never sent to the server. */
  localId: string;
  dayOfWeek: DayOfWeek;
  openTime: string;
  closeTime: string;
}

let _localIdCounter = 0;
function nextLocalId(): string {
  _localIdCounter += 1;
  return `local-${_localIdCounter}`;
}

// ---------------------------------------------------------------------------
// Validation
// ---------------------------------------------------------------------------

interface ValidationErrors {
  [localId: string]: string;
}

/**
 * Returns true if "HH:mm" string `a` is strictly before `b`.
 */
function timeBefore(a: string, b: string): boolean {
  return a < b; // ISO time strings compare lexicographically correctly
}

function validateBlocks(blocks: LocalTimeBlock[]): ValidationErrors {
  const errors: ValidationErrors = {};

  for (const block of blocks) {
    if (!block.openTime || !block.closeTime) {
      errors[block.localId] = 'Both open and close times are required.';
      continue;
    }

    if (!timeBefore(block.openTime, block.closeTime)) {
      errors[block.localId] = 'Close time must be after open time.';
      continue;
    }
  }

  // Check for overlaps within each day
  const byDay = new Map<number, LocalTimeBlock[]>();
  for (const block of blocks) {
    const dayBlocks = byDay.get(block.dayOfWeek) ?? [];
    dayBlocks.push(block);
    byDay.set(block.dayOfWeek, dayBlocks);
  }

  for (const dayBlocks of byDay.values()) {
    // Sort by openTime for overlap detection
    const sorted = [...dayBlocks].sort((a, b) => (a.openTime < b.openTime ? -1 : 1));
    for (let i = 1; i < sorted.length; i++) {
      const prev = sorted[i - 1]!;
      const curr = sorted[i]!;
      // Overlap when current opens before previous closes
      if (prev.closeTime > curr.openTime) {
        errors[curr.localId] = 'Time blocks cannot overlap on the same day.';
      }
    }
  }

  return errors;
}

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function ShopOpeningHours() {
  const { t } = useTranslation('common');
  const navigate = useNavigate();
  const { brandSlug, lang, shopId } = useParams<{
    brandSlug: string;
    lang: string;
    shopId: string;
  }>();

  const resolvedBrandSlug = brandSlug ?? '';
  const resolvedShopId = shopId ?? '';

  const {
    data: openingHoursData,
    isLoading,
    isError,
    error,
  } = useOpeningHours(resolvedBrandSlug, resolvedShopId);

  const setOpeningHours = useSetOpeningHours(resolvedBrandSlug, resolvedShopId);

  const [blocks, setBlocks] = useState<LocalTimeBlock[]>([]);
  const [formInitialized, setFormInitialized] = useState(false);
  const [validationErrors, setValidationErrors] = useState<ValidationErrors>({});
  const [savedMessage, setSavedMessage] = useState(false);

  // Hydrate form from fetched data (only once)
  useEffect(() => {
    if (openingHoursData !== undefined && !formInitialized) {
      setBlocks(
        openingHoursData.timeBlocks.map((tb) => ({
          localId: nextLocalId(),
          dayOfWeek: tb.dayOfWeek as DayOfWeek,
          openTime: tb.openTime,
          closeTime: tb.closeTime,
        })),
      );
      setFormInitialized(true);
    }
  }, [openingHoursData, formInitialized]);

  function addBlock(day: DayOfWeek) {
    setBlocks((prev) => [
      ...prev,
      { localId: nextLocalId(), dayOfWeek: day, openTime: '09:00', closeTime: '17:00' },
    ]);
  }

  function removeBlock(localId: string) {
    setBlocks((prev) => prev.filter((b) => b.localId !== localId));
    setValidationErrors((prev) => {
      const next = { ...prev };
      delete next[localId];
      return next;
    });
  }

  function updateBlock(localId: string, field: 'openTime' | 'closeTime', value: string) {
    setBlocks((prev) =>
      prev.map((b) => (b.localId === localId ? { ...b, [field]: value } : b)),
    );
  }

  function handleSave() {
    const errors = validateBlocks(blocks);
    if (Object.keys(errors).length > 0) {
      setValidationErrors(errors);
      return;
    }

    setValidationErrors({});

    const timeBlocks: TimeBlockRequest[] = blocks.map((b) => ({
      dayOfWeek: b.dayOfWeek,
      openTime: b.openTime,
      closeTime: b.closeTime,
    }));

    setOpeningHours.mutate(
      { timeBlocks },
      {
        onSuccess: () => {
          setSavedMessage(true);
          setTimeout(() => setSavedMessage(false), 3000);
        },
      },
    );
  }

  function handleBack() {
    navigate(`/${brandSlug}/${lang}/admin/shops/${resolvedShopId}`);
  }

  // ---------------------------------------------------------------------------
  // Loading / error states
  // ---------------------------------------------------------------------------

  if (isLoading) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>{t('loading')}</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          {t('error')}:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
        <button onClick={handleBack} style={secondaryButtonStyle}>
          Back
        </button>
      </main>
    );
  }

  // ---------------------------------------------------------------------------
  // Render
  // ---------------------------------------------------------------------------

  return (
    <main style={{ padding: '1.5rem', maxWidth: '48rem' }}>
      {/* Header */}
      <div
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '0.75rem',
          marginBottom: '1.5rem',
        }}
      >
        <button onClick={handleBack} style={secondaryButtonStyle}>
          &larr; Back
        </button>
        <h1 style={{ fontSize: '1.5rem', fontWeight: 700, margin: 0 }}>
          {t('admin.shops.openingHours.title')}
        </h1>
      </div>

      {/* Empty state notice */}
      {blocks.length === 0 && (
        <p
          style={{
            padding: '0.75rem 1rem',
            background: '#fef3c7',
            border: '1px solid #fde68a',
            borderRadius: '0.375rem',
            fontSize: '0.875rem',
            color: '#92400e',
            marginBottom: '1.5rem',
          }}
        >
          {t('admin.shops.openingHours.noBlocks')}
        </p>
      )}

      {/* Day-by-day schedule */}
      {DAYS_ORDER.map((day) => {
        const dayBlocks = blocks.filter((b) => b.dayOfWeek === day);
        return (
          <section
            key={day}
            style={{
              marginBottom: '1.25rem',
              padding: '1rem',
              border: '1px solid #e5e7eb',
              borderRadius: '0.5rem',
              background: '#fff',
            }}
          >
            {/* Day header */}
            <div
              style={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                marginBottom: dayBlocks.length > 0 ? '0.75rem' : 0,
              }}
            >
              <span style={{ fontWeight: 600, fontSize: '0.9375rem' }}>
                {t(DAY_KEY_MAP[day])}
              </span>
              {dayBlocks.length === 0 && (
                <span
                  style={{
                    fontSize: '0.8125rem',
                    color: '#9ca3af',
                    marginRight: '0.5rem',
                  }}
                >
                  {t('admin.shops.openingHours.closed')}
                </span>
              )}
              <button
                type="button"
                onClick={() => addBlock(day)}
                style={addBlockButtonStyle}
              >
                + {t('admin.shops.openingHours.addBlock')}
              </button>
            </div>

            {/* Time blocks for this day */}
            {dayBlocks.map((block) => (
              <div key={block.localId}>
                <div
                  style={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: '0.75rem',
                    marginBottom: '0.5rem',
                    flexWrap: 'wrap',
                  }}
                >
                  {/* Open time */}
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                    <label style={labelStyle}>
                      {t('admin.shops.openingHours.openTime')}
                    </label>
                    <input
                      type="time"
                      value={block.openTime}
                      onChange={(e) => updateBlock(block.localId, 'openTime', e.target.value)}
                      style={timeInputStyle(!!validationErrors[block.localId])}
                    />
                  </div>

                  {/* Close time */}
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '0.25rem' }}>
                    <label style={labelStyle}>
                      {t('admin.shops.openingHours.closeTime')}
                    </label>
                    <input
                      type="time"
                      value={block.closeTime}
                      onChange={(e) => updateBlock(block.localId, 'closeTime', e.target.value)}
                      style={timeInputStyle(!!validationErrors[block.localId])}
                    />
                  </div>

                  {/* Remove button — aligned to bottom of the inputs */}
                  <div style={{ display: 'flex', alignItems: 'flex-end', paddingBottom: '0.125rem' }}>
                    <button
                      type="button"
                      onClick={() => removeBlock(block.localId)}
                      style={removeButtonStyle}
                    >
                      {t('admin.shops.openingHours.remove')}
                    </button>
                  </div>
                </div>

                {/* Validation error for this block */}
                {validationErrors[block.localId] && (
                  <p
                    style={{
                      color: '#dc2626',
                      fontSize: '0.75rem',
                      marginTop: '0.125rem',
                      marginBottom: '0.5rem',
                    }}
                  >
                    {validationErrors[block.localId]}
                  </p>
                )}
              </div>
            ))}
          </section>
        );
      })}

      {/* API error */}
      {setOpeningHours.isError && (
        <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
          {setOpeningHours.error instanceof Error
            ? setOpeningHours.error.message
            : 'Failed to save opening hours. Please try again.'}
        </p>
      )}

      {/* Success message */}
      {savedMessage && (
        <p
          style={{
            color: '#065f46',
            background: '#d1fae5',
            padding: '0.5rem 0.75rem',
            borderRadius: '0.375rem',
            fontSize: '0.875rem',
            marginBottom: '1rem',
          }}
        >
          {t('admin.shops.openingHours.saved')}
        </p>
      )}

      {/* Save button */}
      <button
        type="button"
        onClick={handleSave}
        disabled={setOpeningHours.isPending}
        style={{
          padding: '0.5rem 1.25rem',
          background: '#111827',
          color: '#fff',
          border: 'none',
          borderRadius: '0.375rem',
          cursor: setOpeningHours.isPending ? 'not-allowed' : 'pointer',
          fontWeight: 600,
          opacity: setOpeningHours.isPending ? 0.6 : 1,
        }}
      >
        {setOpeningHours.isPending
          ? t('admin.shops.openingHours.saving')
          : t('admin.shops.openingHours.save')}
      </button>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Style helpers
// ---------------------------------------------------------------------------

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  fontSize: '0.75rem',
  color: '#374151',
};

const secondaryButtonStyle: React.CSSProperties = {
  padding: '0.5rem 1.25rem',
  background: '#fff',
  color: '#374151',
  border: '1px solid #d1d5db',
  borderRadius: '0.375rem',
  cursor: 'pointer',
};

const addBlockButtonStyle: React.CSSProperties = {
  padding: '0.25rem 0.75rem',
  background: '#f9fafb',
  color: '#374151',
  border: '1px solid #d1d5db',
  borderRadius: '0.375rem',
  cursor: 'pointer',
  fontSize: '0.8125rem',
};

const removeButtonStyle: React.CSSProperties = {
  padding: '0.375rem 0.75rem',
  background: '#fff',
  color: '#dc2626',
  border: '1px solid #fca5a5',
  borderRadius: '0.375rem',
  cursor: 'pointer',
  fontSize: '0.8125rem',
};

function timeInputStyle(hasError: boolean): React.CSSProperties {
  return {
    padding: '0.375rem 0.5rem',
    border: `1px solid ${hasError ? '#dc2626' : '#d1d5db'}`,
    borderRadius: '0.375rem',
    fontSize: '0.9375rem',
    boxSizing: 'border-box',
  };
}
