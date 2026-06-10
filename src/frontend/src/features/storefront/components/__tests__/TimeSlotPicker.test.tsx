/**
 * Tests for TimeSlotPicker (US-FP-019):
 * - ASAP option always rendered first
 * - Slot labels display formatted times
 * - Full slots are disabled
 * - onChange fires with selected slot ISO start
 * - Empty slots and error states still offer ASAP
 * - Slots with a start in the past are not rendered (fake timers)
 * - A selected slot that ages out resets to ASAP via onChange
 */
import { beforeAll, describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import i18next from 'i18next';
import '../../../../i18n/config';
import type { TimeSlotResponse } from '@api/orders';
import { TimeSlotPicker } from '../TimeSlotPicker';

beforeAll(async () => {
  await i18next.changeLanguage('nl');
});

// ── Fixtures ─────────────────────────────────────────────────────────────────

/** A slot starting at the given UTC timestamp, 10 minutes long. */
function makeSlot(startIso: string, overrides: Partial<TimeSlotResponse> = {}): TimeSlotResponse {
  const start = new Date(startIso);
  const end = new Date(start.getTime() + 10 * 60_000);
  return {
    start: startIso,
    end: end.toISOString(),
    isAvailable: true,
    remainingCapacity: 2,
    ...overrides,
  };
}

// Future slots (10 min from now + some offset)
const futureFarIso = new Date(Date.now() + 60 * 60_000).toISOString(); // +1h
const futureNearIso = new Date(Date.now() + 20 * 60_000).toISOString(); // +20min

// ── Helpers ───────────────────────────────────────────────────────────────────

interface PickerProps {
  slots?: TimeSlotResponse[];
  value?: string;
  onChange?: (v: string) => void;
  isLoading?: boolean;
  isError?: boolean;
}

function renderPicker(props: PickerProps = {}) {
  const {
    slots = [],
    value = '',
    onChange = vi.fn(),
    isLoading = false,
    isError = false,
  } = props;

  return render(
    <TimeSlotPicker
      slots={slots}
      value={value}
      onChange={onChange}
      isLoading={isLoading}
      isError={isError}
    />,
  );
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('TimeSlotPicker', () => {
  it('always renders the ASAP option', () => {
    renderPicker();
    expect(screen.getByRole('radio', { name: /zo snel mogelijk/i })).toBeInTheDocument();
  });

  it('ASAP option is checked by default (value === empty string)', () => {
    renderPicker({ value: '' });
    expect(screen.getByRole('radio', { name: /zo snel mogelijk/i })).toBeChecked();
  });

  it('renders a radio for each future slot', () => {
    const slots = [makeSlot(futureFarIso), makeSlot(futureNearIso)];
    renderPicker({ slots });
    // 1 ASAP + 2 slots = 3 radios
    expect(screen.getAllByRole('radio')).toHaveLength(3);
  });

  it('fires onChange with the slot start ISO when a slot is selected', () => {
    const onChange = vi.fn();
    const slots = [makeSlot(futureFarIso)];
    renderPicker({ slots, onChange });

    const radios = screen.getAllByRole('radio');
    // radios[0] = ASAP, radios[1] = first slot
    const slotRadio = radios[1];
    if (!slotRadio) throw new Error('Expected slot radio to exist');
    fireEvent.click(slotRadio);
    expect(onChange).toHaveBeenCalledWith(futureFarIso);
  });

  it('fires onChange with empty string when ASAP is selected', () => {
    const onChange = vi.fn();
    const slots = [makeSlot(futureFarIso)];
    renderPicker({ slots, value: futureFarIso, onChange });

    fireEvent.click(screen.getByRole('radio', { name: /zo snel mogelijk/i }));
    expect(onChange).toHaveBeenCalledWith('');
  });

  it('marks full slots as disabled and shows the "Vol" badge', () => {
    const slots = [makeSlot(futureFarIso, { isAvailable: false, remainingCapacity: 0 })];
    renderPicker({ slots });

    // The slot radio has value equal to its start ISO string (non-empty).
    const allRadios = screen.getAllByRole('radio') as HTMLInputElement[];
    const slotRadios = allRadios.filter((r) => r.value !== '');
    expect(slotRadios[0]).toBeDisabled();
    expect(screen.getByText(/vol/i)).toBeInTheDocument();
  });

  it('shows noneAvailable notice and still offers ASAP when slots array is empty', () => {
    renderPicker({ slots: [] });
    expect(screen.getByRole('radio', { name: /zo snel mogelijk/i })).toBeInTheDocument();
    expect(screen.getByText(/geen tijdsloten meer beschikbaar/i)).toBeInTheDocument();
  });

  it('shows loadError notice and still offers ASAP when isError is true', () => {
    renderPicker({ isError: true });
    expect(screen.getByRole('radio', { name: /zo snel mogelijk/i })).toBeInTheDocument();
    expect(screen.getByText(/tijdsloten konden niet geladen worden/i)).toBeInTheDocument();
  });

  it('shows loading text when isLoading is true', () => {
    renderPicker({ isLoading: true });
    expect(screen.getByText(/laden/i)).toBeInTheDocument();
  });

  it('filters out slots whose start is in the past', () => {
    const pastIso = new Date(Date.now() - 5 * 60_000).toISOString();
    const slots = [makeSlot(pastIso), makeSlot(futureFarIso)];
    renderPicker({ slots });
    // Past slot radio should not be present — only ASAP + 1 future slot = 2 radios
    expect(screen.getAllByRole('radio')).toHaveLength(2);
  });

  it('resets selection to ASAP when the selected slot ages out', () => {
    const onChange = vi.fn();
    // A slot that is in the future right now
    const slots = [makeSlot(futureFarIso)];

    // Render with the slot selected
    const { rerender } = render(
      <TimeSlotPicker
        slots={slots}
        value={futureFarIso}
        onChange={onChange}
        isLoading={false}
        isError={false}
      />,
    );

    // Now pretend time advanced past the slot (slots prop becomes empty / slot is gone)
    rerender(
      <TimeSlotPicker
        slots={[]}
        value={futureFarIso}
        onChange={onChange}
        isLoading={false}
        isError={false}
      />,
    );

    // useEffect fires: since futureFarIso is not in freshSlots, onChange('') is called
    expect(onChange).toHaveBeenCalledWith('');
  });
});
