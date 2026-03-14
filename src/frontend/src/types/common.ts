/**
 * Languages supported by the platform.
 * Belgian market: Dutch, French, German.
 */
export type SupportedLocale = 'nl' | 'fr' | 'de';

/**
 * A string value that has been translated into all supported locales.
 */
export type LocalizedString = Record<SupportedLocale, string>;

/**
 * Monetary amount with explicit currency code (ISO 4217).
 */
export interface Money {
  amount: number;
  currency: string;
}

/**
 * Stub for Brand entity — expand as API types are generated.
 */
export interface Brand {
  id: string;
  slug: string;
  name: LocalizedString;
  /** Primary brand color used for theming */
  primaryColor?: string;
}

/**
 * Stub for Shop entity — expand as API types are generated.
 */
export interface Shop {
  id: string;
  brandId: string;
  name: LocalizedString;
  /** Shop-level address */
  address?: string;
}
