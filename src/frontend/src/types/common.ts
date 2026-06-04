/**
 * Languages supported by the platform.
 * Belgian market: Dutch, French, German.
 */
export type SupportedLocale = 'nl' | 'fr' | 'de';

/** Supported two-letter locale codes for runtime validation. */
const SUPPORTED_LOCALES: ReadonlySet<string> = new Set<string>(['nl', 'fr', 'de']);

/**
 * Extracts the two-letter locale from a BCP-47 language tag (e.g. "nl-BE" → "nl").
 * Falls back to 'nl' if the tag is missing or not a supported locale.
 */
export function extractPrimaryLocale(bcp47Tag: string | undefined): SupportedLocale {
  const code = bcp47Tag?.split('-')[0]?.toLowerCase();
  return code && SUPPORTED_LOCALES.has(code) ? (code as SupportedLocale) : 'nl';
}

/**
 * A string value that has been translated into all supported locales.
 *
 * @expected-unused — DTO/response shape used once multilingual product/category features ship (US-FP-022)
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

/** Per-shop eat-in ordering configuration (US-FP-066). */
export interface EatInSettings {
  /** Whether the shop accepts eat-in orders. */
  isEnabled: boolean;
  /** When true (and eat-in is enabled), a table number is mandatory for eat-in orders. */
  requiresTableNumber: boolean;
}

/** Per-shop time-slot ordering configuration (US-FP-020). */
export interface TimeSlotOrderingSettings {
  /** Whether time-slot ordering is enabled. */
  isEnabled: boolean;
  /** Slot length in minutes (5, 10, or 15). Null when disabled. */
  intervalMinutes: number | null;
  /** Maximum orders permitted per slot. Null when disabled. */
  maxOrdersPerInterval: number | null;
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
  /** VAT / enterprise number printed on customer receipts (US-FP-052). Null when not set. */
  vatNumber: string | null;
  isActive: boolean;
  /** When true, new orders are highlighted on the kitchen display (US-FP-026). */
  kitchenDisplayEnabled: boolean;
  /** When true, new orders auto-print on the kitchen display (US-FP-028). */
  ticketPrinterEnabled: boolean;
  /** When true, the kitchen display raises a browser push notification per new order (US-FP-026). */
  pushNotificationEnabled: boolean;
  /** When true, the kitchen display plays a sound alert per new order (US-FP-026). */
  soundAlertEnabled: boolean;
  /** Eat-in ordering configuration (US-FP-066). */
  eatIn: EatInSettings;
  /** Time-slot ordering configuration (US-FP-020). */
  timeSlotOrdering: TimeSlotOrderingSettings;
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

// ── Allergens & Dietary Tags ────────────────────────────────────────────────

/**
 * The 14 EU-regulated allergens (Regulation EU 1169/2011).
 * Values match the backend Allergen enum.
 */
export const Allergen = {
  Gluten: 0,
  Crustaceans: 1,
  Eggs: 2,
  Fish: 3,
  Peanuts: 4,
  Soybeans: 5,
  Milk: 6,
  Nuts: 7,
  Celery: 8,
  Mustard: 9,
  Sesame: 10,
  Sulphites: 11,
  Lupin: 12,
  Molluscs: 13,
} as const;
/** @expected-unused — DTO/response shape used once US-FP-005 (Product CRUD) allergen display ships */
export type Allergen = (typeof Allergen)[keyof typeof Allergen];

/** Label keys for allergens, used for i18n lookup. */
export const ALLERGEN_KEYS = Object.keys(Allergen) as (keyof typeof Allergen)[];

/**
 * Dietary classification tags. Values match the backend DietaryTag enum.
 */
export const DietaryTag = {
  Vegan: 0,
  Vegetarian: 1,
  GlutenFree: 2,
  Halal: 3,
} as const;
/** @expected-unused — DTO/response shape used once US-FP-005 (Product CRUD) dietary tag display ships */
export type DietaryTag = (typeof DietaryTag)[keyof typeof DietaryTag];

/** Label keys for dietary tags, used for i18n lookup. */
export const DIETARY_TAG_KEYS = Object.keys(DietaryTag) as (keyof typeof DietaryTag)[];

/**
 * Product type discriminator: simple or combo.
 */
export type ProductType = 'Simple' | 'Combo';

/**
 * A component product within a combo, including display name and sort order.
 */
export interface ComboItemResponse {
  componentProductId: string;
  name: string;
  sortOrder: number;
}

/**
 * Full product entity as returned by the brand-scoped API.
 */
export interface Product {
  id: string;
  productType: ProductType;
  basePrice: Money;
  imageUrl: string | null;
  menuCategoryId: string | null;
  sortOrderInCategory: number;
  translations: ProductTranslation[];
  allergens: number[];
  dietaryTags: number[];
  comboItems: ComboItemResponse[] | null;
  createdAt: string;
  updatedAt: string;
}

/**
 * Lightweight product list item (name resolved to single language).
 */
export interface ProductListItem {
  id: string;
  productType: ProductType;
  name: string;
  basePrice: Money;
  imageUrl: string | null;
  menuCategoryId: string | null;
  sortOrderInCategory: number;
  allergens: number[];
  dietaryTags: number[];
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

/** @expected-unused — DTO/response shape used once US-FP-003 (Brand theming) font picker ships */
export type PresetFont = (typeof PRESET_FONTS)[number];

// ── Modifier Groups ─────────────────────────────────────────────────────────

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
 * Modifier group assigned to a product, including its modifiers and sort order.
 *
 * NOTE: the admin assignment endpoint (`GET .../products/:id/modifier-groups`)
 * returns ONLY `{ modifierGroupId, sortOrder }` — `name`/`modifiers` are absent
 * there, and `ProductEdit` resolves them from the modifier-group list
 * (useModifierGroups). The storefront/POS menu endpoints DO populate `name` +
 * `modifiers`, so the fields stay required here for those (typed) consumers.
 */
export interface ProductModifierGroupResponse {
  modifierGroupId: string;
  name: string;
  sortOrder: number;
  modifiers: ModifierResponse[];
}

// ── Staff Management ─────────────────────────────────────────────────────────

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

// ── Opening Hours ───────────────────────────────────────────────────────────

/**
 * A single opening hours block for a specific day of the week.
 * dayOfWeek follows .NET DayOfWeek: 0=Sunday, 1=Monday, ..., 6=Saturday.
 */
export interface OpeningHoursTimeBlock {
  id: string;
  dayOfWeek: number; // 0=Sunday, 6=Saturday
  openTime: string;  // "HH:mm"
  closeTime: string; // "HH:mm"
}

/**
 * Request body for a single time block (no id — server assigns it).
 */
export interface TimeBlockRequest {
  dayOfWeek: number;
  openTime: string;
  closeTime: string;
}

/**
 * Request body for PUT /opening-hours — replaces the full weekly schedule.
 */
export interface SetOpeningHoursRequest {
  timeBlocks: TimeBlockRequest[];
}

/**
 * Response from GET /opening-hours.
 */
export interface OpeningHoursResponse {
  timeBlocks: OpeningHoursTimeBlock[];
}

/**
 * Response from GET /status — real-time open/closed state of a shop.
 */
export interface ShopStatusResponse {
  isOpen: boolean;
  nextOpeningTime: string | null;
  timeZoneId: string;
}

// ── Order Lifecycle ─────────────────────────────────────────────────────────

export interface OrderStatusResponse {
  id: string;
  name: string;
  systemKey: string | null;
  sortOrder: number;
  isEnabled: boolean;
  isTerminal: boolean;
  colorHex: string | null;
}

export interface OrderStatusTransitionResponse {
  id: string;
  fromStatusId: string;
  toStatusId: string;
}

export interface OrderLifecycleResponse {
  shopId: string;
  statuses: OrderStatusResponse[];
  transitions: OrderStatusTransitionResponse[];
}

export interface OrderStatusRequest {
  name: string;
  systemKey: string | null;
  sortOrder: number;
  isTerminal: boolean;
  colorHex: string | null;
}

export interface OrderStatusTransitionRequest {
  fromSortOrder: number;
  toSortOrder: number;
}

export interface ConfigureOrderLifecycleRequest {
  statuses: OrderStatusRequest[];
  transitions: OrderStatusTransitionRequest[];
}

// ── Tax Configuration ──────────────────────────────────────────────────────

export type ConsumptionMode = 'Takeaway' | 'EatIn';

export const CONSUMPTION_MODES: readonly ConsumptionMode[] = ['Takeaway', 'EatIn'] as const;

export interface VatRateResponse {
  consumptionMode: ConsumptionMode;
  ratePercentage: number;
}

export interface TaxConfigurationResponse {
  id: string;
  vatRates: VatRateResponse[];
  createdAt: string;
  updatedAt: string;
}

export interface UpdateTaxConfigurationRequest {
  vatRates: VatRateResponse[];
}

export interface TaxBreakdownResponse {
  netAmount: number;
  vatAmount: number;
  grossAmount: number;
  vatRatePercentage: number;
}
