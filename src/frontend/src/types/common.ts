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
 * Authentication methods available for staff on a brand's management portal.
 * Values match the backend StaffAuthMethod enum.
 */
export type StaffAuthMethod = 'EmailPassword' | 'GoogleSso' | 'MicrosoftSso';

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
  staffAuthMethod: StaffAuthMethod;
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
  menuCategoryId: string | null;
  sortOrderInCategory: number;
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
  menuCategoryId: string | null;
  sortOrderInCategory: number;
  createdAt: string;
}

/**
 * Translation of a menu category name into a specific locale.
 */
export interface MenuCategoryTranslation {
  languageCode: SupportedLocale;
  name: string;
}

/**
 * Full menu category entity as returned by the brand-scoped API.
 */
export interface MenuCategory {
  id: string;
  sortOrder: number;
  imageUrl: string | null;
  productCount: number;
  translations: MenuCategoryTranslation[];
  createdAt: string;
  updatedAt: string;
}

/**
 * Lightweight menu category list item (name resolved to single language).
 */
export interface MenuCategoryListItem {
  id: string;
  name: string;
  sortOrder: number;
  imageUrl: string | null;
  productCount: number;
  createdAt: string;
}

/**
 * Platform admin account as returned by the platform API.
 */
export interface PlatformAdmin {
  id: string;
  email: string;
  displayName: string;
  isPlatformAdmin: boolean;
  createdAt: string;
  updatedAt: string;
}

// ── Brand Theming ────────────────────────────────────────────────────────────

/**
 * Brand color palette. All values are CSS hex color strings (e.g. "#2563eb").
 */
export interface BrandColors {
  primary: string;
  secondary: string;
  accent: string;
}

/**
 * Brand typography settings. Font families are selected from the preset list.
 */
export interface BrandTypography {
  headingFontFamily: string;
  bodyFontFamily: string;
}

/**
 * Full brand settings as returned by the settings API.
 */
export interface BrandSettings {
  id: string;
  defaultLanguage: string;
  timezone: string;
  currency: string;
  logoUrl: string | null;
  customDomain: string | null;
  colors: BrandColors | null;
  typography: BrandTypography | null;
  createdAt: string;
  updatedAt: string;
}

/**
 * Lightweight public theme response used by the storefront.
 */
export interface BrandTheme {
  logoUrl: string | null;
  customDomain: string | null;
  primaryColor: string;
  secondaryColor: string;
  accentColor: string;
  headingFontFamily: string;
  bodyFontFamily: string;
}

/**
 * Font families available for brand typography selection.
 */
export const PRESET_FONTS = [
  'System Default',
  'Inter',
  'Roboto',
  'Open Sans',
  'Lato',
  'Poppins',
  'Montserrat',
  'Nunito',
  'Raleway',
  'Source Sans 3',
  'DM Sans',
] as const;

export type PresetFont = (typeof PRESET_FONTS)[number];
