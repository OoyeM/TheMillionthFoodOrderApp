# TheMillionthFoodOrderApp — Product Requirements Document

## Overview

A multi-tenant restaurant CMS and ordering platform. Brands (e.g., Frietjes?, McDonald's) manage menus, pricing, and shops through a central CMS. Each shop (location) can customize products within brand-defined boundaries. Customers order food via an online storefront or in-store via a staff-operated interface.

The platform consists of:
- **CMS Admin Panel** — brand and shop management (menus, products, pricing, staff, settings)
- **Customer-Facing Online Ordering Site** — per-brand/shop storefront for online orders
- **In-Store Ordering Interface** — staff-facing, touch-friendly POS-like screen for counter orders
- **Kitchen Display / Ticket Printer** — order fulfillment view
- **REST API** — .NET backend serving all frontends

First customer: **Frietjes?** — a Belgian fries chain (example: mikeyswebshop.be/menu#frieten).

---

## Tech Stack

- **Backend**: .NET (C#), REST API
- **Frontend**: React (learning project for the developer), PWA (installable web app)
- **Database**: Database-per-brand (each brand gets a fully isolated database; platform owns the data)
- **Auth**: Email/password + SSO (Google, Microsoft) — configurable per brand for staff, customer choice for consumers

---

## Users & Roles

### Platform Level
- **Platform Admin** — manages brands, global settings, platform-wide concerns

### Brand Level
- **Brand Admin** — manages the brand's menu catalog, base pricing, shops, approves shop-level customizations, manages brand staff

### Shop Level
- **Shop Manager** — manages shop-specific settings (opening hours, time slots, order lifecycle, notifications), requests product/pricing overrides
- **Counter Staff** — uses the in-store ordering interface to place orders
- **Kitchen Staff** — views orders on kitchen display, updates order statuses
- **Floor Staff** — general operations, may interact with scheduling

### Customers
- **Registered Customer** — account via email/password or SSO (Google, Microsoft), order history persisted
- **Guest Customer** — orders without creating an account

---

## Multi-Tenant Architecture

### Hierarchy: Platform > Brand > Shop

- **Brand** (e.g., Frietjes?) — defines the base product catalog, pricing, branding/theming, translations, and staff auth method (email/password or SSO)
- **Shop** (e.g., Frietjes? Hasselt) — inherits brand catalog, can:
  - Add custom products (requires brand admin approval)
  - Request price overrides on brand products (requires brand admin approval)
  - Configure shop-specific settings (hours, time slots, order lifecycle, notifications)
  - Personalize within brand theming guidelines

### Data Isolation
- Each brand has its own database — complete data separation
- Platform-level data (brand registry, platform admin accounts) lives in a shared platform database

### Approval Workflow
When a shop creates a custom product or requests a price override:
1. Shop submits request
2. Brand admin reviews — can **approve**, **reject**, or **send back with comments**
3. Shop can revise and resubmit

---

## Product Model

### Product Types

**Simple Product**
- Name, description, base price, image
- Example: "Cola" — €2.50

**Product with Modifiers**
- A product can have modifier groups (sizes, toppings, extras, sauces)
- Each modifier has a name and price adjustment
- Modifiers are additive — sizes, toppings, extras are all the same concept: modifiers on a product
- Example: "Frieten" — base price €3.00, modifiers: Small (€0), Medium (+€1), Large (+€2), Special Sauce (+€0.50)

**Combo Product (Bundle)**
- A predefined bundle linking to underlying products at a set price
- Combos can also have modifiers (size variants, extra toppings)
- Example: "Small Fry Special" — small fries + special sauce = €3.50 (instead of €3.50 separately)
- Provides a "quick select" experience — no wizard, just pick the combo

### Product Ownership
- Brand defines the base catalog with translations (multi-language: NL, FR, DE)
- Shops inherit the full brand catalog
- Shops can add custom products (with translations) — subject to brand approval
- Shops can request price overrides — subject to brand approval

### Allergens & Dietary Info
- Products must support the **14 EU allergens** (legally required in Belgium): gluten, crustaceans, eggs, fish, peanuts, soybeans, milk, nuts, celery, mustard, sesame, sulphites, lupin, molluscs
- Dietary tags: vegan, vegetarian, gluten-free, halal, etc.
- Allergen and dietary info is displayed on the product in the customer-facing storefront
- Brand defines allergen/dietary info for brand products; shops define it for custom products

### Inventory / Stock Tracking
- Shops can optionally set daily stock counts per product (e.g., "50 portions of fries today")
- Stock decrements automatically as orders are placed
- Product auto-disables on the storefront when stock reaches 0
- Stock resets daily (or manually by staff)
- If no stock count is set, the product is treated as unlimited availability
- Products can also be manually toggled as temporarily unavailable (e.g., "out of mayo")

---

## Menu Structure

- Menus are organized into **categories** (e.g., Frieten, Snacks, Sauzen, Dranken)
- Categories contain products
- Categories and product ordering are configurable
- Each brand defines their category structure; shops inherit it

---

## Ordering

### Order Channels
1. **Online** — customer-facing website
2. **In-Store** — staff-facing POS-like interface (touch-friendly, big buttons)

Both channels produce the same order objects and trigger the same fulfillment flow (kitchen display + ticket printer simultaneously).

### Order Types
- **Pickup — pay now** (online payment at order time)
- **Pickup — pay at pickup** (pay when collecting)
- **Eat-in — pay before seating** (order at counter or online, pay immediately, sit at table)
- **Eat-in — pay via QR** (scan QR at table, order from phone, pay online)
- **Delivery — pay now** (online payment; delivery implementation is a basic POC/fake for now)

### Eat-In / Table Support
Shop-level setting — can be enabled or disabled per shop.

**When enabled:**
- Orders are associated with a **table number**
- Multiple separate orders can be linked to the same table (e.g., a group where each person orders and pays individually)
- Kitchen/floor staff can see all orders grouped by table for coordinated preparation and delivery to table
- Table number is required at order time (entered by staff at counter, or auto-assigned via QR code)
- Eat-in orders use **21% VAT** (vs 6% for takeaway)

**QR code flow:**
- Each table has a QR code that links to the shop's ordering page with the table number pre-filled
- Customer scans, orders, pays — food is brought to the table

### Time Slot System
Shops can enable or disable time-slot ordering.

**When enabled:**
- Shop configures: interval (5 / 10 / 15 min) and max orders per interval
- Example: interval = 15 min, max = 3 → slots at 10:00 (3 spots), 10:15 (3 spots), etc.
- Customer picks a time slot during checkout
- "As soon as possible" is also an option

**When disabled:**
- Orders are first-come-first-served
- Show estimated waiting time based on shop-configured estimates:
  - Shop sets: small order (≤X items) = Y min, medium order (≤X items) = Y min, large order (≤X items) = Y min
  - If no estimates configured, show "place in line" only

### Order Lifecycle
Each shop defines its own order statuses and transitions.

**Default lifecycle:** Placed → Confirmed → Preparing → Ready → Picked Up / Delivered

**Shops can customize:**
- Pick from predefined statuses and toggle which ones they use
- Define their own custom statuses
- Example (simple fry shop): Placed → Done (they print the ticket, move paper around, throw it out)

### Notifications
When an online order comes in, notification method is configurable per shop:
- Kitchen display update
- Ticket printer
- Push notification
- Sound alert
- Any combination — shop decides what fits their workflow

---

## Branding & Theming

- Each brand gets a custom look and feel on the customer-facing site
- Configurable: colors, logo, typography, custom domain
- Shops operate within the brand's theming

---

## Multi-Language Support

- Platform supports Dutch, French, German (Belgian market)
- Brands provide translations for their product catalog and UI strings
- Shops provide translations for their custom products
- Customer-facing site language is selectable

---

## Staff & Scheduling

### Basic Shift Management
- Create and assign shifts to staff members
- View schedule (daily/weekly)

### Advanced Features
- Staff availability management
- Shift swap requests
- Hour tracking

Scheduling is per-shop. Brand admins have visibility across shops.

---

## Authentication

### Staff / Admin
- Configurable per brand:
  - Email/password (for small mom-and-pop shops)
  - SSO via Microsoft or Google (for larger brands)

### Customers
- Customer chooses:
  - Email/password
  - SSO via Google or Microsoft
- Guest checkout (no account required)

---

## Opening Hours & Availability

- Shops set their opening hours
- Online ordering automatically closes when shop is closed
- Special hours / holiday overrides
- Products can be toggled unavailable per shop in real-time

---

## Reporting & Analytics

Dashboard available in the CMS for brand admins and shop managers:

- **Sales**: daily/weekly/monthly order counts and revenue
- **Products**: best-selling products, highest-profit products, least-sold products
- **Orders**: average order value, orders by channel (online vs in-store), orders by type (pickup vs delivery)
- **Time**: peak ordering hours, average preparation time
- **Inventory signals**: frequently unavailable products

---

## Tax Handling

- Belgian VAT rules: **6% for takeaway**, **21% for eat-in**
- The system handles the distinction — order type (pickup/delivery vs eat-in) determines the VAT rate
- Tax is calculated and displayed on receipts and in reporting
- Configurable per brand/region if expanded beyond Belgium

---

## Loyalty & Promotions

### Stamp System
- Customers earn points per order (configurable: points per euro spent or per order)
- Points can be redeemed for purchases (configurable redemption rates)
- Points balance visible in customer account

### Discount Codes
- **Multi-use codes**: a code usable once per customer (e.g., "SUMMER10" — 10% off, each customer can use it once)
- **Single-use codes**: a unique code for one specific customer (e.g., birthday discount)
- Codes can apply: percentage off, fixed amount off, free product
- Codes can have expiry dates and minimum order requirements

---

## Receipts

- **Digital receipts** via email (for registered customers and guests who provide email)
- **Printed receipts** at pickup/in-store
- Receipts include: order items, modifiers, prices, VAT breakdown, payment method, order number

---

## Offline Mode (In-Store)

- The in-store ordering interface must work when internet connection drops
- Orders are queued locally and synced when connectivity is restored
- Kitchen display / ticket printer continues to function locally
- Conflict resolution: server timestamps take precedence on sync

---

## Progressive Web App (PWA)

- The customer-facing site is installable as a PWA (add to home screen)
- No native mobile apps planned — the responsive PWA is the mobile strategy
- Service worker for offline caching of menu data and static assets
- Push notifications for order status updates (when customer has installed the PWA)

---

## Payments

Payment processing is **faked/mocked for now** — the flow and UI should be built as if real payments exist (select payment method, confirmation screen, etc.) but the actual payment gateway integration is out of scope.

---

## Delivery

Basic POC only — the UI flow exists (customer selects delivery, enters address) but actual delivery logistics and third-party integration are out of scope.

---

## Scope — Customer-Facing URL Model

Each shop has its own customer-facing URL. A customer browsing one shop only sees that shop's menu. There is no cross-shop cart — ordering is always scoped to a single shop.

---

## Out of Scope (for now)

- **Order modification/cancellation** after placement — not supported in v1
- **Native mobile apps** — PWA is the mobile strategy
- **Real payment gateway integration** — faked/mocked
- **Real delivery logistics** — basic POC only
- **SMS notifications** — may be added later as an additional notification channel
