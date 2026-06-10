import { apiClient } from './client';

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface TimeSlotDto {
  /** ISO-8601 UTC datetime string for the slot start. */
  slotStart: string;
  /** Shop-local "HH:mm" label. */
  label: string;
  /** False when the slot has reached maxOrdersPerInterval. */
  isAvailable: boolean;
}

export interface TimeSlotAvailabilityResponse {
  /** Reflects configuration only — never the open/closed state (design decision 3). */
  isEnabled: boolean;
  intervalMinutes: number | null;
  slots: TimeSlotDto[];
  /**
   * Active (non-terminal) order count for place-in-line display.
   * Only populated when isEnabled is false (AC5).
   */
  activeOrderCount: number | null;
}

// ---------------------------------------------------------------------------
// API module
// ---------------------------------------------------------------------------

/**
 * API functions for time-slot availability (US-FP-019).
 * Route: GET /brands/{brandSlug}/shops/{shopId}/time-slots
 */
export const timeSlotsApi = {
  get: (brandSlug: string, shopId: string): Promise<TimeSlotAvailabilityResponse> =>
    apiClient
      .get<TimeSlotAvailabilityResponse>(`/brands/${brandSlug}/shops/${shopId}/time-slots`)
      .then((r) => r.data),
};
