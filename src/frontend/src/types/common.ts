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
 * Staff roles within the platform hierarchy.
 * Values match the backend StaffRole enum.
 * Brand-level roles (BrandAdmin, Customer) require BrandId but no ShopId.
 * Shop-level roles (ShopManager, CounterStaff, KitchenStaff, FloorStaff) require both BrandId and ShopId.
 */
export type StaffRole =
  | 'BrandAdmin'
  | 'ShopManager'
  | 'CounterStaff'
  | 'KitchenStaff'
  | 'FloorStaff'
  | 'Customer';

/** Numeric values matching the backend StaffRole enum for use in POST requests. */
export const StaffRoleValue: Record<StaffRole, number> = {
  BrandAdmin: 0,
  ShopManager: 1,
  CounterStaff: 2,
  KitchenStaff: 3,
  FloorStaff: 4,
  Customer: 5,
};

/** Shop-level roles that require a ShopId when assigning. */
export const ShopLevelRoles: ReadonlySet<StaffRole> = new Set<StaffRole>([
  'ShopManager',
  'CounterStaff',
  'KitchenStaff',
  'FloorStaff',
]);

/**
 * A single brand staff role assignment as returned by the brand-scoped API.
 * One user may appear multiple times if they hold multiple roles.
 */
export interface StaffMember {
  /** Platform user ID of the staff member. */
  id: string;
  email: string;
  displayName: string;
  /** The unique ID of this specific role assignment (used for deactivation). */
  roleId: string;
  /** Numeric role value matching the backend StaffRole enum. */
  role: number;
  shopId: string | null;
  shopName: string | null;
  createdAt: string;
}
