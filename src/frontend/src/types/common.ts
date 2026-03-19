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

/**
 * Translation of a modifier or modifier group name into a specific locale.
 */
export interface ModifierTranslationResponse {
  languageCode: string;
  name: string;
}

/**
 * A single modifier option within a modifier group.
 * PriceAdjustment can be negative (e.g. discount).
 */
export interface ModifierResponse {
  id: string;
  priceAdjustment: number;
  sortOrder: number;
  translations: ModifierTranslationResponse[];
}

/**
 * Full modifier group entity as returned by the brand-scoped API.
 */
export interface ModifierGroupResponse {
  id: string;
  translations: ModifierTranslationResponse[];
  modifiers: ModifierResponse[];
  createdAt: string;
  updatedAt: string;
}

/**
 * Lightweight modifier group list item (name resolved to single language).
 */
export interface ModifierGroupListItem {
  id: string;
  name: string;
  modifierCount: number;
  productCount: number;
  createdAt: string;
}

/**
 * Modifier group assigned to a product, including its modifiers and sort order on the product.
 */
export interface ProductModifierGroupResponse {
  modifierGroupId: string;
  name: string;
  sortOrder: number;
  modifiers: ModifierResponse[];
}
