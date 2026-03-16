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
 * Brand entity as returned by the platform API.
 */
export interface Brand {
  id: string;
  slug: string;
  name: string;
  contactEmail: string;
  contactPhone: string | null;
  isActive: boolean;
  databaseName: string;
  createdAt: string;
  updatedAt: string;
}

/**
 * Physical address of a shop (Belgian market, structured per EU postal standards).
 */
export interface ShopAddress {
  street: string;
  number: string;
  city: string;
  postalCode: string;
  country: string;
}

/**
 * Shop entity as returned by the brand-scoped API.
 * Shops belong to a single brand and are stored in the brand's isolated database.
 */
export interface Shop {
  id: string;
  name: string;
  slug: string;
  address: ShopAddress;
  contactEmail: string;
  contactPhone: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

/**
 * Translation of a product name/description into a specific locale.
 */
export interface ProductTranslation {
  languageCode: SupportedLocale;
  name: string;
  description: string | null;
}

/**
 * Full product entity as returned by the brand-scoped API.
 */
export interface Product {
  id: string;
  basePrice: Money;
  imageUrl: string | null;
  translations: ProductTranslation[];
  createdAt: string;
  updatedAt: string;
}

/**
 * Lightweight product list item (name resolved to single language).
 */
export interface ProductListItem {
  id: string;
  name: string;
  basePrice: Money;
  imageUrl: string | null;
  createdAt: string;
}
