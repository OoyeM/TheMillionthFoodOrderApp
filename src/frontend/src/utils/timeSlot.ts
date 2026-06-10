/**
 * Time formatting utilities (US-FP-019).
 */

// Hoisted: Intl.DateTimeFormat construction is expensive and the picker formats
// hundreds of slots per render. nl-BE gives the 24-hour HH:mm clock used across
// the Belgian NL/FR/DE markets.
const timeFormat = new Intl.DateTimeFormat('nl-BE', { hour: '2-digit', minute: '2-digit' });

/**
 * Formats a UTC ISO timestamp as a wall-clock time (HH:mm) in the client's
 * local timezone — devices and customers share the shop's Belgian timezone.
 */
export function formatTime(iso: string): string {
  return timeFormat.format(new Date(iso));
}

/**
 * Formats a UTC ISO time-slot start/end pair as a human-readable time range.
 *
 * @example formatTimeSlot('2026-06-10T11:30:00Z', '2026-06-10T11:40:00Z') → "13:30–13:40"
 */
export function formatTimeSlot(startIso: string, endIso: string): string {
  return `${formatTime(startIso)}–${formatTime(endIso)}`;
}
