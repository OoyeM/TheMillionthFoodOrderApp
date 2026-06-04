import { describe, it, expect } from 'vitest';
import { playNewOrderSound, primeAudioAlerts } from '../playNewOrderSound';

// jsdom provides no Web Audio API, so these exercise the guard / no-op path.
// The invariant under test: a missing AudioContext must never throw.
describe('playNewOrderSound (no Web Audio API)', () => {
  it('never throws when AudioContext is unavailable', () => {
    expect(() => { primeAudioAlerts(); }).not.toThrow();
    expect(() => { playNewOrderSound(); }).not.toThrow();
  });
});
