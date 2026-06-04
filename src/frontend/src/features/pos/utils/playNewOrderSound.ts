/**
 * Sound-alert helper for new kitchen orders (US-FP-026).
 *
 * Synthesises a short chime with the Web Audio API rather than shipping an audio
 * asset, so there is nothing to license or bundle. No-ops when the Web Audio API
 * is unavailable (tests/SSR/old browsers) and never throws — a failed chime must
 * never break the order board.
 */

type AudioContextCtor = typeof AudioContext;

// Created lazily and reused: kept module-level so the kitchen page can prime it
// from a user gesture (browsers start the context suspended until then) and so a
// burst of new orders never allocates more than one context.
let sharedContext: AudioContext | null = null;

function getAudioContextCtor(): AudioContextCtor | undefined {
  if (typeof window === 'undefined') return undefined;
  const w = window as typeof window & { webkitAudioContext?: AudioContextCtor };
  // eslint-disable-next-line @typescript-eslint/no-unnecessary-condition -- window.AudioContext is typed as always-present but is genuinely absent in older browsers/SSR; the ?? falls back to the prefixed ctor.
  return w.AudioContext ?? w.webkitAudioContext;
}

function ensureContext(): AudioContext | null {
  const Ctor = getAudioContextCtor();
  if (Ctor === undefined) return null;
  if (sharedContext === null) {
    try {
      sharedContext = new Ctor();
    } catch {
      return null;
    }
  }
  return sharedContext;
}

/**
 * Resumes (or creates) the shared AudioContext from a user gesture. Browsers keep
 * the context "suspended" until a gesture occurs, so the kitchen page calls this
 * from its "enable alerts" control before any auto-played chime.
 */
export function primeAudioAlerts(): void {
  const ctx = ensureContext();
  if (ctx === null) return;
  if (ctx.state === 'suspended') void ctx.resume();
}

function playBeep(ctx: AudioContext, frequency: number, startAt: number, durationSec: number): void {
  const osc = ctx.createOscillator();
  const gain = ctx.createGain();
  osc.type = 'sine';
  osc.frequency.value = frequency;
  // Quick attack + exponential release so it reads as a chime, not a buzz.
  gain.gain.setValueAtTime(0.0001, startAt);
  gain.gain.exponentialRampToValueAtTime(0.3, startAt + 0.01);
  gain.gain.exponentialRampToValueAtTime(0.0001, startAt + durationSec);
  osc.connect(gain);
  gain.connect(ctx.destination);
  osc.start(startAt);
  osc.stop(startAt + durationSec + 0.02);
}

/**
 * Plays a short two-tone chime to announce a new order (US-FP-026). No-ops when
 * the Web Audio API is unavailable. Safe to call rapidly.
 */
export function playNewOrderSound(): void {
  const ctx = ensureContext();
  if (ctx === null) return;
  if (ctx.state === 'suspended') void ctx.resume();
  try {
    const now = ctx.currentTime;
    // Two ascending beeps — distinctive over kitchen noise without being harsh.
    playBeep(ctx, 880, now, 0.18);
    playBeep(ctx, 1320, now + 0.2, 0.22);
  } catch {
    // Ignore — a failed chime must never break the order board.
  }
}
