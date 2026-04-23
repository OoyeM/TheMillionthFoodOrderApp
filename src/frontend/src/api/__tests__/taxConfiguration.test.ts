import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { taxConfigurationApi } from '../taxConfiguration';

const BRAND_SLUG = 'frietjes';

/**
 * Tests for src/api/taxConfiguration.ts
 */
describe('taxConfigurationApi', () => {
  describe('get', () => {
    it('returns the tax configuration for a brand', async () => {
      const config = await taxConfigurationApi.get(BRAND_SLUG);

      expect(config).toMatchObject({ id: 'tax-1' });
      expect(config.vatRates).toHaveLength(2);
      expect(config.vatRates).toContainEqual({ consumptionMode: 'Takeaway', ratePercentage: 6 });
      expect(config.vatRates).toContainEqual({ consumptionMode: 'EatIn', ratePercentage: 21 });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/brands/:slug/tax-configuration', () =>
          new HttpResponse(null, { status: 401 }),
        ),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await taxConfigurationApi.get(BRAND_SLUG);
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('update', () => {
    it('updates the VAT rates and returns the updated config', async () => {
      const updated = await taxConfigurationApi.update(BRAND_SLUG, {
        vatRates: [
          { consumptionMode: 'Takeaway', ratePercentage: 9 },
          { consumptionMode: 'EatIn', ratePercentage: 21 },
        ],
      });

      expect(updated.vatRates).toContainEqual({
        consumptionMode: 'Takeaway',
        ratePercentage: 9,
      });
    });
  });

  describe('calculate', () => {
    it('calculates VAT breakdown for takeaway consumption mode', async () => {
      const result = await taxConfigurationApi.calculate(BRAND_SLUG, 10.6, 'Takeaway');

      // 6% VAT on 10.60 gross: VAT = 10.60 * 6/106 ≈ 0.60; net ≈ 10.00
      expect(result.grossAmount).toBe(10.6);
      expect(result.vatRatePercentage).toBe(6);
      expect(result.vatAmount).toBeGreaterThan(0);
      expect(result.netAmount + result.vatAmount).toBeCloseTo(result.grossAmount, 1);
    });

    it('calculates VAT breakdown for eat-in consumption mode', async () => {
      const result = await taxConfigurationApi.calculate(BRAND_SLUG, 12.1, 'EatIn');

      expect(result.vatRatePercentage).toBe(21);
      expect(result.grossAmount).toBe(12.1);
    });
  });
});
