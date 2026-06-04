import { useEffect, useRef } from 'react';
import type { ReactNode } from 'react';
import { Outlet, useParams } from 'react-router-dom';
import { useBrandTheme, defaultTheme } from '../hooks/useBrandTheme';
import type { BrandTheme } from '../../../types/common';

/** Google Fonts families that require a network request. */
const SYSTEM_FONT_LABELS = new Set(['System Default', '']);

/**
 * Injects brand CSS custom properties onto the <html> element and loads Google Fonts
 * when non-system font families are configured.
 *
 * Custom properties injected:
 * - --brand-color-primary
 * - --brand-color-secondary
 * - --brand-color-accent
 * - --brand-font-heading
 * - --brand-font-body
 *
 * Usage: use as a layout component in React Router (renders <Outlet /> inside),
 * or pass children directly.
 *
 * Theme flicker on initial load is acceptable for MVP; SSR optimisation can follow.
 */
export function ThemeProvider({ children }: { children?: ReactNode }) {
  const { brandSlug } = useParams<{ brandSlug: string }>();
  const resolvedSlug = brandSlug ?? '';

  const { data: theme } = useBrandTheme(resolvedSlug);
  const effectiveTheme = theme ?? defaultTheme;

  // Track which Google Fonts link elements we've inserted so we can clean up
  const fontLinksRef = useRef<Map<string, HTMLLinkElement>>(new Map());

  useEffect(() => {
    applyThemeCssProperties(effectiveTheme);
  }, [effectiveTheme]);

  useEffect(() => {
    loadGoogleFonts(effectiveTheme, fontLinksRef.current);
  }, [effectiveTheme.headingFontFamily, effectiveTheme.bodyFontFamily]);

  // Cleanup font link elements on unmount
  useEffect(() => {
    const links = fontLinksRef.current;
    return () => {
      links.forEach((el) => { el.remove(); });
      links.clear();
    };
  }, []);

  // Render children if provided (direct usage), otherwise render Outlet (layout route usage)
  return <>{children ?? <Outlet />}</>;
}

// ── Helpers ──────────────────────────────────────────────────────────────────

function applyThemeCssProperties(theme: BrandTheme): void {
  const root = document.documentElement;
  root.style.setProperty('--brand-color-primary', theme.primaryColor);
  root.style.setProperty('--brand-color-secondary', theme.secondaryColor);
  root.style.setProperty('--brand-color-accent', theme.accentColor);

  const headingStack =
    SYSTEM_FONT_LABELS.has(theme.headingFontFamily)
      ? 'system-ui, sans-serif'
      : `'${theme.headingFontFamily}', system-ui, sans-serif`;

  const bodyStack =
    SYSTEM_FONT_LABELS.has(theme.bodyFontFamily)
      ? 'system-ui, sans-serif'
      : `'${theme.bodyFontFamily}', system-ui, sans-serif`;

  root.style.setProperty('--brand-font-heading', headingStack);
  root.style.setProperty('--brand-font-body', bodyStack);
}

function loadGoogleFonts(
  theme: BrandTheme,
  links: Map<string, HTMLLinkElement>,
): void {
  const fontsToLoad = new Set<string>();

  if (!SYSTEM_FONT_LABELS.has(theme.headingFontFamily)) {
    fontsToLoad.add(theme.headingFontFamily);
  }
  if (!SYSTEM_FONT_LABELS.has(theme.bodyFontFamily)) {
    fontsToLoad.add(theme.bodyFontFamily);
  }

  if (fontsToLoad.size === 0) return;

  // Build a single Google Fonts request for all needed families
  const families = Array.from(fontsToLoad)
    .map((f) => `family=${encodeURIComponent(f)}:wght@400;600;700`)
    .join('&');

  const href = `https://fonts.googleapis.com/css2?${families}&display=swap`;
  const cacheKey = Array.from(fontsToLoad).sort().join(',');

  if (links.has(cacheKey)) return; // already loaded

  // Preconnect hints (add once)
  ensurePreconnect('https://fonts.googleapis.com');
  ensurePreconnect('https://fonts.gstatic.com', true);

  const link = document.createElement('link');
  link.rel = 'stylesheet';
  link.href = href;
  document.head.appendChild(link);
  links.set(cacheKey, link);
}

function ensurePreconnect(origin: string, crossOrigin = false): void {
  const existing = document.querySelector(`link[rel="preconnect"][href="${origin}"]`);
  if (existing) return;

  const link = document.createElement('link');
  link.rel = 'preconnect';
  link.href = origin;
  if (crossOrigin) link.crossOrigin = 'anonymous';
  document.head.appendChild(link);
}
