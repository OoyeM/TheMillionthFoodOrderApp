import { describe, it, expect } from 'vitest';
import { formatTimeSlot } from '../timeSlot';

describe('formatTimeSlot', () => {
  it('formats a start/end UTC ISO pair as a time range in nl-BE format', () => {
    // 2026-07-10 is in CEST (+02:00), so 15:30 UTC = 17:30 local, 15:40 UTC = 17:40 local
    const result = formatTimeSlot('2026-07-10T15:30:00Z', '2026-07-10T15:40:00Z');
    // The exact output depends on the system timezone, but it must contain the separator.
    expect(result).toContain('–');
    // Should contain two colon-separated time parts (HH:mm format).
    const parts = result.split('–');
    expect(parts).toHaveLength(2);
    expect(parts[0]).toMatch(/^\d{2}:\d{2}$/);
    expect(parts[1]).toMatch(/^\d{2}:\d{2}$/);
  });

  it('produces different start and end times when the slot spans multiple minutes', () => {
    const result = formatTimeSlot('2026-07-10T10:00:00Z', '2026-07-10T10:15:00Z');
    const parts = result.split('–');
    expect(parts[0]).not.toBe(parts[1]);
  });

  it('produces the same time for both ends when the slot has zero duration', () => {
    const result = formatTimeSlot('2026-07-10T12:00:00Z', '2026-07-10T12:00:00Z');
    const parts = result.split('–');
    expect(parts[0]).toBe(parts[1]);
  });
});
