/**
 * Converts a display name into a URL-safe slug: lowercase, trimmed, with
 * runs of non-alphanumeric characters collapsed to single hyphens and
 * leading/trailing hyphens stripped. Shared by the brand and shop create pages.
 */
export function nameToSlug(name: string): string {
  return name
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}
