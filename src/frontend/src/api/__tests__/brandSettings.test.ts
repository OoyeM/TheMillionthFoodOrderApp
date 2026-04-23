import { describe, it, expect, vi } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { brandSettingsApi } from '../brandSettings';

/**
 * Tests for src/api/brandSettings.ts
 */
describe('brandSettingsApi', () => {
  describe('get', () => {
    it('returns brand settings', async () => {
      const settings = await brandSettingsApi.get('frietjes');

      expect(settings).toMatchObject({
        id: 'settings-1',
        defaultLanguage: 'nl',
        timezone: 'Europe/Brussels',
        currency: 'EUR',
      });
    });

    it('dispatches auth:session-expired on 401', async () => {
      server.use(
        http.get('/api/brands/:slug/settings', () => new HttpResponse(null, { status: 401 })),
      );

      const listener = vi.fn();
      window.addEventListener('auth:session-expired', listener);

      try {
        await brandSettingsApi.get('frietjes');
      } catch {
        // Expected
      } finally {
        window.removeEventListener('auth:session-expired', listener);
      }

      expect(listener).toHaveBeenCalledOnce();
    });
  });

  describe('updateTheming', () => {
    it('updates theming and returns updated settings', async () => {
      const settings = await brandSettingsApi.updateTheming('frietjes', {
        colors: { primary: '#ff0000', secondary: '#00ff00', accent: '#0000ff' },
        typography: { headingFontFamily: 'Inter', bodyFontFamily: 'Roboto' },
        customDomain: 'www.frietjes.be',
      });

      expect(settings).toMatchObject({ customDomain: 'www.frietjes.be' });
      expect(settings.colors).toMatchObject({ primary: '#ff0000' });
    });
  });

  describe('uploadLogo', () => {
    it('uploads a logo file and returns the logo URL', async () => {
      // Mock the multipart handler to return a logo URL
      server.use(
        http.post('/api/brands/:slug/settings/logo', () =>
          HttpResponse.json({ logoUrl: 'https://cdn.example.com/custom-logo.png' }),
        ),
      );

      const file = new File(['(logo)'], 'logo.png', { type: 'image/png' });
      const response = await brandSettingsApi.uploadLogo('frietjes', file);

      expect(response.logoUrl).toBe('https://cdn.example.com/custom-logo.png');
    });
  });

  describe('getTheme', () => {
    it('returns the public brand theme', async () => {
      const theme = await brandSettingsApi.getTheme('frietjes');

      expect(theme).toMatchObject({
        primaryColor: '#2563eb',
        headingFontFamily: 'Inter',
      });
    });
  });
});
