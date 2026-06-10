import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import type { TimeSlotResponse } from '@api/orders';
import { formatTimeSlot } from '../../../utils/timeSlot';

// ---------------------------------------------------------------------------
// TimeSlotPicker — radio group for selecting an available time slot (US-FP-019)
// ---------------------------------------------------------------------------

interface TimeSlotPickerProps {
  /** Available slots from the server. May be empty near closing time. */
  slots: TimeSlotResponse[];
  /**
   * Currently selected slot start (ISO string), or empty string meaning "ASAP".
   * Empty string is the initial/reset value.
   */
  value: string;
  /** Called when the customer selects a slot. `''` means "ASAP". */
  onChange: (startIso: string) => void;
  /** True while the slot list is being fetched. */
  isLoading: boolean;
  /** True when the slot-list fetch failed. */
  isError: boolean;
}

/**
 * Renders a radio group for time-slot selection.
 *
 * - ASAP is always the first option (AC3).
 * - Full slots are shown but disabled (AC2).
 * - Slots whose `start` is no longer in the future are filtered out at render
 *   time (stale-slot guard).  If the currently selected slot ages out, the
 *   selection is reset to ASAP.
 * - Empty state and error state both still offer ASAP so checkout stays usable.
 */
export function TimeSlotPicker({
  slots,
  value,
  onChange,
  isLoading,
  isError,
}: TimeSlotPickerProps) {
  const { t } = useTranslation('common');
  const now = Date.now();

  // Filter out slots that are no longer in the future at render time.
  const freshSlots = slots.filter((s) => new Date(s.start).getTime() > now);

  // Reset to ASAP if the previously selected slot has aged out of the list,
  // and tell the customer — silently downgrading their slot to ASAP would let
  // them submit believing the slot is still booked.
  const [showResetNotice, setShowResetNotice] = useState(false);
  const selectionAgedOut = value !== '' && !freshSlots.some((s) => s.start === value);
  useEffect(() => {
    if (selectionAgedOut) {
      setShowResetNotice(true);
      onChange('');
    }
  }, [selectionAgedOut, onChange]);

  function handleSelect(radioValue: string) {
    setShowResetNotice(false);
    onChange(radioValue);
  }

  // ── Loading state ──────────────────────────────────────────────────────────
  if (isLoading) {
    return (
      <div style={{ marginBottom: '1.5rem' }}>
        <p style={{ fontSize: '0.9375rem', color: '#6b7280' }}>
          {t('loading')}
        </p>
      </div>
    );
  }

  // ── Shared radio option renderer ───────────────────────────────────────────

  function renderOption(
    key: string,
    radioValue: string,
    label: string,
    disabled = false,
    badge?: string,
  ) {
    const isSelected = value === radioValue;
    return (
      <label
        key={key}
        style={{
          display: 'flex',
          alignItems: 'center',
          gap: '0.75rem',
          padding: '0.875rem 1rem',
          borderRadius: '0.5rem',
          border: `2px solid ${isSelected ? 'var(--brand-color-primary, #111827)' : '#e5e7eb'}`,
          background: isSelected ? '#f9fafb' : '#fff',
          cursor: disabled ? 'not-allowed' : 'pointer',
          fontSize: '0.9375rem',
          fontWeight: isSelected ? 600 : 400,
          color: '#111827',
          opacity: disabled ? 0.5 : 1,
        }}
      >
        <input
          type="radio"
          name="timeSlotPicker"
          value={radioValue}
          checked={isSelected}
          disabled={disabled}
          onChange={() => { handleSelect(radioValue); }}
          style={{ width: '1.125rem', height: '1.125rem', cursor: disabled ? 'not-allowed' : 'pointer' }}
          aria-disabled={disabled}
        />
        <span style={{ flex: 1 }}>{label}</span>
        {badge && (
          <span
            style={{
              display: 'inline-block',
              padding: '0.125rem 0.5rem',
              borderRadius: '999px',
              background: '#f3f4f6',
              color: '#6b7280',
              fontSize: '0.75rem',
              fontWeight: 700,
            }}
          >
            {badge}
          </span>
        )}
      </label>
    );
  }

  const asapOption = renderOption('asap', '', t('storefront.checkout.timeSlot.asap'));

  return (
    <fieldset style={{ border: 'none', padding: 0, margin: '0 0 1.5rem' }}>
      <legend
        style={{
          fontSize: '1rem',
          fontWeight: 700,
          color: '#111827',
          marginBottom: '0.75rem',
        }}
      >
        {t('storefront.checkout.timeSlot.label')}
      </legend>

      {/* Notice when the previously selected slot aged out and was reset to ASAP */}
      {showResetNotice && (
        <p
          role="status"
          style={{
            color: '#991b1b',
            background: '#fef2f2',
            border: '1px solid #fecaca',
            borderRadius: '0.5rem',
            padding: '0.625rem 0.875rem',
            fontSize: '0.875rem',
            margin: '0 0 0.5rem',
          }}
        >
          {t('storefront.checkout.timeSlot.slotFull')}
        </p>
      )}

      {/* ASAP is always pinned above the scrollable slot list */}
      <div style={{ marginBottom: '0.5rem' }}>
        {asapOption}
      </div>

      {/* Error state — ASAP still available above */}
      {isError && (
        <p style={{ color: '#6b7280', fontSize: '0.875rem', marginTop: '0.5rem' }}>
          {t('storefront.checkout.timeSlot.loadError')}
        </p>
      )}

      {/* Empty state (no future slots) — ASAP still available above */}
      {!isError && freshSlots.length === 0 && (
        <p style={{ color: '#6b7280', fontSize: '0.875rem', marginTop: '0.5rem' }}>
          {t('storefront.checkout.timeSlot.noneAvailable')}
        </p>
      )}

      {/* Slot list in a scrollable container so hundreds of slots don't break the page */}
      {!isError && freshSlots.length > 0 && (
        <div
          style={{
            maxHeight: '16rem',
            overflowY: 'auto',
            display: 'flex',
            flexDirection: 'column',
            gap: '0.5rem',
            paddingRight: '0.25rem',
          }}
        >
          {freshSlots.map((slot) =>
            renderOption(
              slot.start,
              slot.start,
              formatTimeSlot(slot.start, slot.end),
              !slot.isAvailable,
              !slot.isAvailable ? t('storefront.checkout.timeSlot.full') : undefined,
            ),
          )}
        </div>
      )}
    </fieldset>
  );
}
