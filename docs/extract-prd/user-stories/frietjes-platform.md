# User Stories: Frietjes Platform

## Summary

This document contains the complete set of user stories for the Frietjes Platform, a multi-tenant restaurant CMS and ordering system targeting the Belgian market. The stories cover the full product scope: multi-tenant architecture (Platform > Brand > Shop), product catalog management with modifiers and combos, multi-channel ordering (online and in-store), kitchen fulfillment, branding/theming, multi-language support (NL/FR/DE), staff scheduling, authentication, reporting, Belgian tax handling, loyalty programs, receipts, offline mode, PWA capabilities, mocked payments, and a basic delivery POC. User roles include Platform Admin, Brand Admin, Shop Manager, Counter Staff, Kitchen Staff, Floor Staff, Registered Customer, Guest Customer, and System.

---

## Epic: Multi-Tenant Architecture

### US-FP-001: Create and manage brands
**As a** Platform Admin, **I want** to create, edit, and deactivate brands on the platform, **so that** each restaurant chain operates as an isolated tenant.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Platform Admin can create a new brand with name, slug, and contact details
- [ ] Creating a brand provisions a dedicated database for that brand
- [ ] Platform Admin can edit brand metadata (name, contact info)
- [ ] Platform Admin can deactivate a brand, which disables all its shops and storefronts
- [ ] Brand registry is stored in the shared platform database
- [ ] Deactivated brands cannot accept new orders

**Notes:** Database provisioning strategy (automated vs. manual) is an implementation detail to be decided during design.

---

### US-FP-002: Create and manage shops within a brand
**As a** Brand Admin, **I want** to create, edit, and deactivate shops under my brand, **so that** each physical location can operate independently.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Brand Admin can create a shop with name, address, and contact details
- [ ] Each shop inherits the brand's full product catalog upon creation
- [ ] Brand Admin can edit shop metadata
- [ ] Brand Admin can deactivate a shop, hiding it from customers
- [ ] Each shop gets a unique customer-facing URL
- [ ] Shop data is stored in the brand's isolated database

---

### US-FP-003: Assign brand-level staff authentication method
**As a** Brand Admin, **I want** to configure whether staff authenticate via email/password or SSO (Google/Microsoft), **so that** authentication fits the brand's operational style.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Brand Admin can select email/password or SSO (Google or Microsoft) as the staff auth method
- [ ] The chosen method applies to all staff under the brand (Brand Admin, Shop Manager, Counter Staff, Kitchen Staff, Floor Staff)
- [ ] Switching auth method prompts a confirmation warning
- [ ] Staff login screens reflect the configured method

---

### US-FP-004: Data isolation between brands
**As a** Platform Admin, **I want** each brand's data to be stored in a separate database, **so that** there is complete data isolation between tenants.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Each brand's products, orders, customers, staff, and settings reside in its own database
- [ ] Platform-level data (brand registry, platform admin accounts) lives in a shared platform database
- [ ] No API endpoint leaks data across brand boundaries
- [ ] A shop in Brand A cannot access data from Brand B

---

## Epic: Product Management

### US-FP-005: Create and manage simple products
**As a** Brand Admin, **I want** to create simple products with name, description, base price, and image, **so that** they appear in the brand catalog for all shops.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Brand Admin can create a product with name, description, base price (in EUR), and image upload
- [ ] Product name and description support translations (NL, FR, DE)
- [ ] Product appears in all shops under the brand upon creation
- [ ] Brand Admin can edit and soft-delete products
- [ ] Deleted products are hidden from storefronts but retained for historical order records

---

### US-FP-006: Add modifier groups to products
**As a** Brand Admin, **I want** to attach modifier groups (sizes, toppings, extras, sauces) to a product, **so that** customers can customize their order.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Brand Admin can create a modifier group with a name (e.g., "Size", "Sauces") and assign it to one or more products
- [ ] Each modifier within a group has a name and a price adjustment (can be zero, positive, or negative)
- [ ] Modifier names support translations (NL, FR, DE)
- [ ] Multiple modifier groups can be attached to a single product (e.g., Size + Sauce)
- [ ] The final price is the base price plus the sum of all selected modifier adjustments
- [ ] Example: "Frieten" base EUR 3.00 with modifiers Small (+EUR 0), Medium (+EUR 1), Large (+EUR 2), Special Sauce (+EUR 0.50)

---

### US-FP-007: Create and manage combo products
**As a** Brand Admin, **I want** to create combo (bundle) products that group underlying products at a set price, **so that** customers get a quick-select deal.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Brand Admin can create a combo product linking to two or more existing products
- [ ] Combo has its own name, description, image, and fixed bundle price
- [ ] Combos can also have modifier groups (e.g., size variant for the whole combo)
- [ ] The combo provides a "quick select" experience: customer picks the combo, not individual items through a wizard
- [ ] Example: "Small Fry Special" = small fries + special sauce = EUR 3.50

---

### US-FP-008: Manage allergen and dietary information
**As a** Brand Admin, **I want** to tag products with EU allergens and dietary labels, **so that** the platform meets Belgian legal requirements and informs customers.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] All 14 EU allergens are available as tags: gluten, crustaceans, eggs, fish, peanuts, soybeans, milk, nuts, celery, mustard, sesame, sulphites, lupin, molluscs
- [ ] Dietary tags are available: vegan, vegetarian, gluten-free, halal (extensible list)
- [ ] Brand Admin can assign allergen and dietary tags to brand products
- [ ] Allergen and dietary info is displayed on the product detail in the customer-facing storefront
- [ ] Shop Managers can assign allergen/dietary info to shop-level custom products

---

### US-FP-009: Shop adds custom products (with approval)
**As a** Shop Manager, **I want** to add custom products specific to my shop, **so that** I can offer local specialities not in the brand catalog.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Shop Manager can create a custom product with name, description, price, image, translations, allergen info, and dietary tags
- [ ] The custom product is submitted for brand admin approval (status: pending)
- [ ] Custom product is not visible on the storefront until approved
- [ ] Brand Admin receives a notification or sees pending requests in the CMS
- [ ] Brand Admin can approve, reject, or send back with comments
- [ ] Shop Manager can revise a rejected or sent-back product and resubmit
- [ ] Approved custom products appear only in that shop's storefront

---

### US-FP-010: Shop requests price override on brand product
**As a** Shop Manager, **I want** to request a different price for a brand product in my shop, **so that** I can adjust pricing to local market conditions.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Shop Manager can select a brand product and submit a price override request with the desired new price and a justification
- [ ] The request follows the same approval workflow as custom products (pending > approved/rejected/sent back)
- [ ] Approved overrides apply only to that shop's storefront
- [ ] The original brand price remains unchanged for other shops
- [ ] Brand Admin can revoke a previously approved override

---

### US-FP-011: Brand admin reviews approval requests
**As a** Brand Admin, **I want** to review, approve, reject, or request changes on shop-submitted custom products and price overrides, **so that** I maintain catalog quality and pricing consistency.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Brand Admin sees a list of pending approval requests across all shops
- [ ] Each request shows: shop name, product details, requested change, justification
- [ ] Brand Admin can approve (product/override goes live), reject (with reason), or send back with comments
- [ ] Shop Manager is notified of the decision
- [ ] Approval history is retained for audit

---

### US-FP-012: Set daily stock count per product per shop
**As a** Shop Manager, **I want** to set optional daily stock counts for products, **so that** the storefront automatically hides sold-out items.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Shop Manager can set a stock count (integer) for any product in their shop
- [ ] Stock decrements by the ordered quantity when an order is placed
- [ ] When stock reaches 0, the product is automatically hidden/disabled on the storefront
- [ ] Stock resets daily at a configurable time (default: shop opening time)
- [ ] Shop Manager or Counter Staff can manually reset or adjust stock at any time
- [ ] If no stock count is set, the product has unlimited availability

---

### US-FP-013: Toggle product availability in real-time
**As a** Shop Manager, **I want** to manually mark a product as temporarily unavailable, **so that** customers cannot order items we have run out of mid-day.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Shop Manager or Counter Staff can toggle any product to "unavailable" from the CMS or in-store interface
- [ ] Unavailable products are greyed out or hidden on the customer-facing storefront
- [ ] The product can be toggled back to available at any time
- [ ] Toggling is instant and does not require page reload for customers currently browsing

**Notes:** Consider WebSocket or polling for real-time storefront updates.

---

## Epic: Menu Structure

### US-FP-014: Define menu categories
**As a** Brand Admin, **I want** to create and order menu categories (e.g., Frieten, Snacks, Sauzen, Dranken), **so that** products are logically grouped on the storefront.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Brand Admin can create categories with a name (translated: NL, FR, DE) and optional icon/image
- [ ] Categories can be reordered via drag-and-drop or position number
- [ ] Products are assigned to categories
- [ ] Shops inherit the brand's category structure
- [ ] Categories with no available products are hidden from the storefront

---

### US-FP-015: Order products within categories
**As a** Brand Admin, **I want** to control the display order of products within a category, **so that** featured or popular items appear first.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Brand Admin can reorder products within a category via drag-and-drop or position number
- [ ] The configured order is reflected on the customer-facing storefront
- [ ] Newly added products default to the end of the category

---

## Epic: Ordering

### US-FP-016: Place an online order (customer storefront)
**As a** Registered Customer, **I want** to browse a shop's menu, add items to my cart, and place an order online, **so that** I can order food remotely.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Customer navigates to a shop-specific URL and sees the shop's menu organized by categories
- [ ] Customer can add simple products, products with modifiers, and combos to the cart
- [ ] Cart displays itemized list with prices including modifier adjustments
- [ ] Customer selects order type: pickup (pay now / pay at pickup), eat-in (pay before seating / pay via QR), or delivery (pay now)
- [ ] Customer completes checkout (address for delivery, time slot if enabled, payment)
- [ ] Order is created with a unique order number and enters the order lifecycle
- [ ] Ordering is scoped to a single shop; no cross-shop cart

---

### US-FP-017: Place an order as a guest
**As a** Guest Customer, **I want** to place an order without creating an account, **so that** I can order quickly without registration friction.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Guest can browse menu and add items to cart
- [ ] At checkout, guest provides name and optionally email (for digital receipt) and phone
- [ ] No account is created; order is not linked to a persistent profile
- [ ] Guest can select all available order types
- [ ] Guest receives an order confirmation page with order number

---

### US-FP-018: Place an in-store order (counter staff)
**As** Counter Staff, **I want** to use a touch-friendly POS-like interface to take customer orders in-store, **so that** walk-in customers are served efficiently.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] In-store interface displays the shop's menu with large, touch-friendly buttons
- [ ] Counter Staff can add products (with modifiers) and combos to the order
- [ ] Counter Staff selects order type: pickup (pay at pickup) or eat-in
- [ ] For eat-in, Counter Staff enters the table number
- [ ] Order is submitted and triggers the same fulfillment flow as online orders (kitchen display + ticket printer)
- [ ] Interface works on tablet-sized screens

---

### US-FP-019: Select time slot at checkout
**As a** Registered Customer, **I want** to pick a time slot for my order when the shop has time-slot ordering enabled, **so that** I know when my food will be ready.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] When time-slot ordering is enabled, checkout shows available slots based on the shop's configured interval and max orders per interval
- [ ] Full slots are greyed out and not selectable
- [ ] "As soon as possible" is always available as an option
- [ ] Selected time slot is stored with the order and visible to kitchen staff
- [ ] When time-slot ordering is disabled, customer sees estimated wait time (or place in line) instead

---

### US-FP-020: Configure time slot settings
**As a** Shop Manager, **I want** to enable time-slot ordering and configure the interval and max orders per slot, **so that** order flow matches my kitchen's capacity.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Shop Manager can toggle time-slot ordering on/off
- [ ] When enabled, Shop Manager sets interval (5, 10, or 15 minutes) and max orders per interval
- [ ] Changes take effect for new orders immediately
- [ ] Example: interval = 15 min, max = 3 produces slots at 10:00 (3 spots), 10:15 (3 spots), etc.

---

### US-FP-021: Configure estimated wait times
**As a** Shop Manager, **I want** to set estimated preparation times by order size, **so that** customers without time slots see a wait estimate.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Shop Manager defines thresholds: small order (up to X items) = Y min, medium order (up to X items) = Y min, large order (up to X items) = Y min
- [ ] When time-slot ordering is disabled, the storefront displays the estimated wait time based on the order size
- [ ] If no estimates are configured, the storefront shows "place in line" (queue position) only

---

### US-FP-022: Configure order lifecycle statuses
**As a** Shop Manager, **I want** to define which order statuses my shop uses and their transitions, **so that** the workflow fits my kitchen's process.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] A default lifecycle is provided: Placed > Confirmed > Preparing > Ready > Picked Up / Delivered
- [ ] Shop Manager can enable/disable predefined statuses
- [ ] Shop Manager can create custom statuses with a name and position in the flow
- [ ] The minimum lifecycle is two statuses (e.g., Placed > Done)
- [ ] Kitchen display and customer-facing order tracking reflect the configured statuses

---

### US-FP-023: Update order status (kitchen)
**As** Kitchen Staff, **I want** to advance an order's status on the kitchen display, **so that** counter/floor staff and customers know when food is ready.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Kitchen display shows incoming orders with items, modifiers, order type, and table number (if eat-in)
- [ ] Kitchen Staff can tap to advance an order to the next status in the lifecycle
- [ ] Status change is reflected in real-time on the customer's order tracking page
- [ ] Status change triggers any configured notifications (push, sound, etc.)

---

### US-FP-024: Eat-in ordering with table number
**As a** Registered Customer, **I want** to place an eat-in order associated with my table number, **so that** food is delivered to my table.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Eat-in option is only available when the shop has eat-in enabled
- [ ] Customer or Counter Staff enters a table number at order time
- [ ] Multiple separate orders can be linked to the same table number
- [ ] Kitchen/floor staff can view orders grouped by table
- [ ] Eat-in orders are taxed at 21% VAT

---

### US-FP-025: QR code table ordering
**As a** Registered Customer, **I want** to scan a QR code at my table to open the shop's ordering page with my table number pre-filled, **so that** I can order from my seat without queuing.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Each table's QR code encodes a URL pointing to the shop's storefront with a table number parameter
- [ ] Scanning the QR code opens the storefront with table number pre-filled and order type set to eat-in
- [ ] Customer proceeds through normal ordering flow (browse, add to cart, checkout, pay)
- [ ] The order is associated with the correct table number
- [ ] Shop Manager can generate/print QR codes for each table from the CMS

---

### US-FP-026: Configure order notifications
**As a** Shop Manager, **I want** to choose how the shop is notified of new orders (kitchen display, ticket printer, push notification, sound alert), **so that** notifications match our workflow.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Shop Manager can enable/disable each notification method independently: kitchen display update, ticket printer, push notification, sound alert
- [ ] Multiple methods can be active simultaneously
- [ ] New orders trigger all enabled notification methods
- [ ] Both online and in-store orders trigger the same notifications

---

### US-FP-027: View order on kitchen display
**As** Kitchen Staff, **I want** to see new and active orders on a dedicated kitchen display screen, **so that** I can prepare food in the correct sequence.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Kitchen display shows orders sorted by time (oldest first) or by time slot
- [ ] Each order card shows: order number, items with modifiers, order type (pickup/eat-in/delivery), table number (if eat-in), time slot (if applicable)
- [ ] New orders appear automatically without page refresh
- [ ] Completed orders are removed from the active view

---

### US-FP-028: Print order ticket
**As** Counter Staff, **I want** orders to print automatically on the ticket printer, **so that** the kitchen has a physical ticket for each order.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] When ticket printer notification is enabled, new orders trigger automatic printing
- [ ] Ticket includes: order number, items with modifiers, order type, table number (if eat-in), time slot (if applicable), timestamp
- [ ] Counter Staff can manually reprint a ticket from the in-store interface

---

## Epic: Branding & Theming

### US-FP-029: Configure brand theming
**As a** Brand Admin, **I want** to set brand colors, logo, typography, and custom domain for the customer-facing site, **so that** the storefront reflects my brand identity.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Brand Admin can upload a logo and set primary/secondary/accent colors
- [ ] Brand Admin can select or upload fonts (or choose from a preset list)
- [ ] Brand Admin can configure a custom domain (e.g., order.frietjes.be)
- [ ] All shops under the brand inherit and operate within this theming
- [ ] Changes to theming are reflected on the storefront after save (no deploy needed)

---

## Epic: Multi-Language Support

### US-FP-030: Provide translations for product catalog
**As a** Brand Admin, **I want** to enter product names and descriptions in Dutch, French, and German, **so that** customers see the menu in their preferred language.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Every product name and description field accepts input in NL, FR, and DE
- [ ] At least one language (the brand's primary language) is required; others are optional
- [ ] Missing translations fall back to the primary language
- [ ] Translations apply to categories, modifier names, and combo names as well

---

### US-FP-031: Select language on the storefront
**As a** Registered Customer, **I want** to switch the storefront language between Dutch, French, and German, **so that** I can browse and order in my preferred language.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] A language selector is visible on the storefront (e.g., NL / FR / DE toggle)
- [ ] Switching language translates all UI strings, product names, descriptions, category names
- [ ] Language preference is persisted for registered customers across sessions
- [ ] Default language is determined by browser locale or brand default

---

## Epic: Staff & Scheduling

### US-FP-032: Manage staff accounts
**As a** Brand Admin, **I want** to create and manage staff accounts with roles (Shop Manager, Counter Staff, Kitchen Staff, Floor Staff), **so that** each person has the correct access.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Brand Admin can invite staff by email, assigning them a role and a shop
- [ ] Staff roles determine feature access (e.g., Counter Staff cannot access CMS reporting)
- [ ] Brand Admin can deactivate staff accounts
- [ ] Shop Manager can view staff assigned to their shop

---

### US-FP-033: Create and assign shifts
**As a** Shop Manager, **I want** to create shifts and assign them to staff, **so that** the shop has adequate coverage.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Shop Manager can create a shift with date, start time, end time, and required role
- [ ] Shop Manager can assign one or more staff members to a shift
- [ ] Staff members can view their assigned shifts in a daily/weekly schedule view
- [ ] Brand Admin has visibility into schedules across all shops

---

### US-FP-034: Manage staff availability
**As** Floor Staff, **I want** to submit my availability for scheduling, **so that** the Shop Manager can schedule shifts around my constraints.

**Priority:** Could Have

**Acceptance Criteria:**
- [ ] Staff can mark available/unavailable time blocks per day
- [ ] Availability is visible to the Shop Manager when creating shifts
- [ ] System warns if a shift is assigned to unavailable staff

---

### US-FP-035: Request shift swap
**As** Counter Staff, **I want** to request swapping a shift with a colleague, **so that** I can handle schedule changes flexibly.

**Priority:** Could Have

**Acceptance Criteria:**
- [ ] Staff can select an assigned shift and request a swap with a specific colleague
- [ ] The colleague receives a notification and can accept or decline
- [ ] Shop Manager is notified of completed swaps
- [ ] Swap is only finalized after colleague acceptance

---

### US-FP-036: Track staff hours
**As a** Shop Manager, **I want** to see tracked hours per staff member, **so that** I can manage payroll and utilization.

**Priority:** Could Have

**Acceptance Criteria:**
- [ ] Hours are tracked based on assigned shifts (scheduled hours)
- [ ] Shop Manager can view hours per staff member for a selected period (week/month)
- [ ] Export to CSV is available

---

## Epic: Authentication

### US-FP-037: Customer registration and login
**As a** Registered Customer, **I want** to create an account via email/password or SSO (Google/Microsoft) and log in, **so that** my order history and preferences are saved.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Customer can register with email and password
- [ ] Customer can register/log in via Google SSO
- [ ] Customer can register/log in via Microsoft SSO
- [ ] Duplicate email addresses across auth methods are handled (account linking or clear error)
- [ ] After login, customer sees their order history

---

### US-FP-038: Guest checkout
**As a** Guest Customer, **I want** to complete an order without creating an account, **so that** I avoid unnecessary registration.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Guest provides name at checkout; email is optional (for digital receipt)
- [ ] No account is created
- [ ] Guest sees an order confirmation with order number
- [ ] Guest cannot view past orders or earn loyalty points

---

### US-FP-039: Staff login with configured auth method
**As** Counter Staff, **I want** to log in using the method configured for my brand (email/password or SSO), **so that** I can access the in-store interface.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Staff login page dynamically shows email/password or SSO button based on brand configuration
- [ ] Successful login redirects to the appropriate interface based on role (CMS for admin/manager, in-store for counter staff, kitchen display for kitchen staff)
- [ ] Failed login shows a clear error message
- [ ] Session timeout is configurable

---

## Epic: Opening Hours & Availability

### US-FP-040: Set shop opening hours
**As a** Shop Manager, **I want** to set regular weekly opening hours for my shop, **so that** the storefront only accepts orders during business hours.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Shop Manager can set opening and closing times for each day of the week
- [ ] A shop can have multiple time blocks per day (e.g., 11:00-14:00 and 17:00-22:00)
- [ ] Online ordering is automatically disabled outside opening hours
- [ ] The storefront shows the shop's current status (open/closed) and next opening time

---

### US-FP-041: Set special hours and holiday overrides
**As a** Shop Manager, **I want** to set special opening hours for holidays or events, **so that** hours are accurate on exceptional days.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Shop Manager can create a date-specific override (e.g., Dec 25 = closed, Dec 31 = 10:00-16:00)
- [ ] Overrides take precedence over regular weekly hours
- [ ] Overrides are visible in a calendar view in the CMS
- [ ] The storefront reflects overridden hours on the specified date

---

## Epic: Reporting & Analytics

### US-FP-042: View sales dashboard
**As a** Brand Admin, **I want** to see daily, weekly, and monthly sales data (order count and revenue), **so that** I can track business performance.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Dashboard shows order count and total revenue for selected period (day/week/month)
- [ ] Data can be filtered by shop
- [ ] Brand Admin sees aggregated data across all shops and can drill down per shop
- [ ] Shop Manager sees only their shop's data

---

### US-FP-043: View product performance report
**As a** Brand Admin, **I want** to see best-selling, highest-profit, and least-sold products, **so that** I can optimize the menu.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Report shows products ranked by units sold, revenue generated, and profit (if cost data is available)
- [ ] Report can be filtered by date range and shop
- [ ] Least-sold products are flagged to help identify candidates for removal

---

### US-FP-044: View order analytics
**As a** Shop Manager, **I want** to see average order value, orders by channel (online vs. in-store), and orders by type (pickup/eat-in/delivery), **so that** I understand ordering patterns.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Dashboard shows average order value for selected period
- [ ] Orders are broken down by channel (online vs. in-store) with counts and percentages
- [ ] Orders are broken down by type (pickup, eat-in, delivery)
- [ ] Peak ordering hours are visualized (e.g., bar chart by hour of day)
- [ ] Average preparation time is shown (time from Placed to Ready)

---

### US-FP-045: View inventory signals
**As a** Shop Manager, **I want** to see which products are frequently marked unavailable or sell out quickly, **so that** I can improve stock planning.

**Priority:** Could Have

**Acceptance Criteria:**
- [ ] Report shows products that hit zero stock most frequently
- [ ] Report shows products most often toggled to "temporarily unavailable"
- [ ] Data can be filtered by date range

---

## Epic: Tax Handling

### US-FP-046: Apply Belgian VAT rates based on order type
**As a** System, **I want** to apply 6% VAT for takeaway orders and 21% VAT for eat-in orders, **so that** the platform complies with Belgian tax law.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Pickup and delivery orders are taxed at 6%
- [ ] Eat-in orders (both counter and QR) are taxed at 21%
- [ ] VAT rate is determined by order type, not product
- [ ] VAT amount is calculated per line item and shown on the receipt
- [ ] VAT breakdown (net, VAT amount, gross) is included on receipts and in reporting
- [ ] Tax rates are configurable per brand/region for future expansion beyond Belgium

---

## Epic: Loyalty & Promotions

### US-FP-047: Configure stamp/loyalty program
**As a** Brand Admin, **I want** to set up a loyalty program where customers earn points, **so that** repeat purchases are rewarded.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Brand Admin can enable/disable the loyalty program
- [ ] Brand Admin configures earning rule: points per euro spent OR points per order
- [ ] Brand Admin configures redemption rate (e.g., 100 points = EUR 1 discount)
- [ ] Only registered customers earn and redeem points
- [ ] Points are earned after order completion (not at placement)

---

### US-FP-048: View and redeem loyalty points
**As a** Registered Customer, **I want** to see my points balance and redeem points at checkout, **so that** I benefit from my loyalty.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Customer account page shows current points balance and transaction history
- [ ] At checkout, customer can choose to redeem available points for a discount
- [ ] Redeemed points are deducted from the balance
- [ ] Discount from points is shown as a line item on the receipt

---

### US-FP-049: Create multi-use discount codes
**As a** Brand Admin, **I want** to create discount codes usable once per customer, **so that** I can run promotional campaigns.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Brand Admin can create a code (e.g., "SUMMER10") with: discount type (percentage, fixed amount, or free product), value, expiry date, and optional minimum order amount
- [ ] Each registered customer can use the code once; the system blocks repeated use
- [ ] Guest customers can use multi-use codes (tracked by email if provided, otherwise one-time use per session)
- [ ] The code is validated at checkout and discount is applied to the order total
- [ ] Expired codes are rejected with a clear message

---

### US-FP-050: Create single-use discount codes
**As a** Brand Admin, **I want** to generate unique single-use codes for specific customers (e.g., birthday discount), **so that** I can offer personalized promotions.

**Priority:** Could Have

**Acceptance Criteria:**
- [ ] Brand Admin can generate a unique code tied to a specific customer (by email or account)
- [ ] The code can only be used once and only by the designated customer
- [ ] Code supports: percentage off, fixed amount off, or free product
- [ ] Code can have an expiry date and minimum order requirement
- [ ] After use, the code is marked as redeemed

---

## Epic: Receipts

### US-FP-051: Generate digital receipt
**As a** Registered Customer, **I want** to receive a digital receipt via email after my order is completed, **so that** I have a record of my purchase.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Digital receipt is sent to the registered customer's email upon order completion
- [ ] Guest customers receive a receipt if they provided an email at checkout
- [ ] Receipt includes: order number, order items with modifiers, individual prices, subtotal, VAT breakdown (rate, net, VAT amount, gross), total, payment method, date/time, shop name and address
- [ ] Receipt is formatted as a readable email (not just a raw text dump)

---

### US-FP-052: Print receipt at point of sale
**As** Counter Staff, **I want** to print a receipt for the customer at pickup or after in-store payment, **so that** walk-in customers receive a physical record.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Receipt is printed on the receipt printer (standard thermal 80mm format)
- [ ] Printed receipt includes the same information as the digital receipt
- [ ] Counter Staff can trigger a reprint from the in-store interface
- [ ] Receipt includes legal requirements (shop name, VAT number, date, etc.)

---

## Epic: Offline Mode

### US-FP-053: In-store interface works offline
**As** Counter Staff, **I want** the in-store ordering interface to continue working when the internet connection drops, **so that** walk-in orders are not disrupted.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] When connectivity is lost, the in-store interface remains functional (menu browsing, order creation)
- [ ] Orders placed offline are queued locally (in browser storage / IndexedDB)
- [ ] When connectivity is restored, queued orders are automatically synced to the server
- [ ] A visible indicator shows online/offline status
- [ ] Conflict resolution uses server timestamps on sync

---

### US-FP-054: Kitchen display works offline
**As** Kitchen Staff, **I want** the kitchen display to continue showing orders when the internet drops, **so that** food preparation is not interrupted.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Orders already loaded on the kitchen display remain visible during an outage
- [ ] Orders placed via the in-store interface (also offline) are relayed locally to the kitchen display
- [ ] Status updates made on the kitchen display offline are synced when connectivity returns

**Notes:** Local relay between in-store interface and kitchen display during offline mode needs architectural design (e.g., local network, service worker, or shared local storage).

---

## Epic: Progressive Web App (PWA)

### US-FP-055: Install storefront as PWA
**As a** Registered Customer, **I want** to install the shop's storefront on my phone's home screen as a PWA, **so that** I get an app-like experience without downloading from an app store.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] The customer-facing site serves a valid web app manifest
- [ ] Browser shows "Add to Home Screen" prompt on supported devices
- [ ] Installed PWA launches in standalone mode (no browser chrome)
- [ ] PWA icon and splash screen use the brand's logo and colors

---

### US-FP-056: Offline caching of menu data
**As a** Registered Customer, **I want** the storefront to cache menu data and static assets, **so that** I can browse the menu even with a poor connection.

**Priority:** Could Have

**Acceptance Criteria:**
- [ ] A service worker caches the menu, product images, and static assets after first visit
- [ ] Cached menu is served when the device is offline or has poor connectivity
- [ ] Cache is refreshed when the customer revisits with connectivity
- [ ] A notice is shown if the customer is viewing potentially stale data

---

### US-FP-057: Push notifications for order status
**As a** Registered Customer, **I want** to receive push notifications when my order status changes, **so that** I know when my food is ready without checking the app.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Customer is prompted to allow push notifications after installing the PWA (or on first order)
- [ ] Push notifications are sent on each order status transition (e.g., "Your order is being prepared", "Your order is ready for pickup")
- [ ] Notifications include the order number and new status
- [ ] Customer can disable push notifications in their account settings

---

## Epic: Payments

### US-FP-058: Complete payment flow (mocked)
**As a** Registered Customer, **I want** to go through a full payment flow at checkout (select method, confirm, receive confirmation), **so that** the ordering experience feels complete even though actual payment processing is mocked.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Checkout presents payment method options (e.g., credit card, Bancontact, cash at pickup)
- [ ] Selecting an online payment method shows a mock payment screen that always succeeds after a brief delay
- [ ] "Pay at pickup" skips the payment screen and confirms the order immediately
- [ ] Order confirmation page shows payment status (paid / pay on pickup)
- [ ] The payment flow UI is built to be replaceable by a real payment gateway without structural changes

**Notes:** Real payment gateway integration (e.g., Mollie, Stripe) is out of scope for v1.

---

## Epic: Delivery (POC)

### US-FP-059: Place a delivery order (basic POC)
**As a** Registered Customer, **I want** to select delivery as an order type and enter my delivery address, **so that** the platform supports delivery flow end-to-end in a basic form.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Customer can select "Delivery" as order type at checkout
- [ ] Customer enters a delivery address (street, number, postal code, city)
- [ ] Address is validated for basic format (not empty, has required fields)
- [ ] Order is created with type "delivery" and the address is stored
- [ ] Delivery fee is configurable per shop (can be EUR 0)
- [ ] Delivery orders are taxed at 6% (takeaway VAT)

**Notes:** Actual delivery logistics, driver assignment, and third-party integration are out of scope. This is a UI/flow POC only.

---

## Epic: Platform Administration

### US-FP-060: Platform admin dashboard
**As a** Platform Admin, **I want** to see a dashboard listing all brands with key metrics, **so that** I can monitor platform health.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Dashboard lists all brands with status (active/inactive) and number of shops
- [ ] Platform Admin can navigate to any brand's admin panel
- [ ] Platform Admin can create new brands and manage platform-level settings

---

### US-FP-061: Manage platform admin accounts
**As a** Platform Admin, **I want** to create and manage other platform admin accounts, **so that** platform operations are not dependent on a single person.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Platform Admin can invite new platform admins by email
- [ ] Platform Admin can deactivate other platform admin accounts
- [ ] At least one platform admin must exist at all times (prevent last-admin deletion)

---

## Epic: Customer Experience

### US-FP-062: View order history
**As a** Registered Customer, **I want** to view my past orders, **so that** I can reorder favourites or track my spending.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Customer account page shows a list of past orders sorted by date (newest first)
- [ ] Each order shows: order number, date, items, total, status
- [ ] Customer can view order details including receipt information
- [ ] Order history is scoped to a single brand (not cross-brand)

---

### US-FP-063: Track current order status
**As a** Registered Customer, **I want** to see the real-time status of my current order, **so that** I know when to pick up or expect delivery.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] After placing an order, customer sees an order tracking page
- [ ] The page shows the current status in the shop's configured lifecycle (e.g., Placed > Preparing > Ready)
- [ ] Status updates appear in real-time without manual page refresh
- [ ] Guest customers can track their order via the order confirmation page (using order number)

---

### US-FP-064: Browse shop menu with allergen and dietary filters
**As a** Registered Customer, **I want** to filter the menu by allergens and dietary preferences, **so that** I can quickly find safe food options.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Storefront provides filter options for allergens (exclude products containing selected allergens)
- [ ] Storefront provides filter options for dietary tags (show only vegan, vegetarian, etc.)
- [ ] Filters can be combined (e.g., vegetarian AND no nuts)
- [ ] Product cards display allergen icons and dietary tags

---

## Epic: QR Code Management

### US-FP-065: Generate QR codes for tables
**As a** Shop Manager, **I want** to generate and print QR codes for each table in my shop, **so that** customers can scan and order from their seat.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Shop Manager can specify the number of tables in the shop
- [ ] System generates a unique QR code per table, encoding the shop storefront URL with table number parameter
- [ ] QR codes can be downloaded as printable images (PDF or PNG)
- [ ] QR codes can be regenerated (e.g., if the URL scheme changes)
- [ ] Each QR code is labelled with the table number

---

## Epic: Shop Configuration

### US-FP-066: Enable or disable eat-in ordering
**As a** Shop Manager, **I want** to toggle eat-in ordering on or off for my shop, **so that** the storefront only shows relevant order types.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Shop Manager can toggle eat-in ordering in shop settings
- [ ] When disabled, eat-in options and table numbers are hidden from both the storefront and in-store interface
- [ ] When enabled, eat-in order types appear and table number entry is required
- [ ] QR code table ordering is only functional when eat-in is enabled

---

### US-FP-067: Configure custom domain for brand storefront
**As a** Brand Admin, **I want** to point a custom domain (e.g., order.frietjes.be) to my brand's storefront, **so that** customers access ordering via a branded URL.

**Priority:** Should Have

**Acceptance Criteria:**
- [ ] Brand Admin can enter a custom domain in brand settings
- [ ] Platform provides DNS configuration instructions (CNAME or A record)
- [ ] SSL certificate is provisioned for the custom domain (automated via Let's Encrypt or similar)
- [ ] The storefront is accessible via both the custom domain and the default platform URL

**Notes:** SSL provisioning strategy needs architectural decision.

---

## Epic: System & Infrastructure

### US-FP-068: Real-time order updates via WebSocket or SSE
**As a** System, **I want** to push order status changes and new order notifications in real-time, **so that** kitchen displays, customer tracking pages, and notification systems stay current without polling.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Kitchen display receives new orders and status changes in real-time
- [ ] Customer order tracking page receives status updates in real-time
- [ ] In-store interface receives updates to order statuses in real-time
- [ ] Connection automatically reconnects after drops

**Notes:** Technology choice (WebSocket vs. Server-Sent Events vs. SignalR) is an implementation decision.

---

### US-FP-069: API serves all frontends via REST
**As a** System, **I want** a single .NET REST API backend serving the CMS, customer storefront, in-store interface, and kitchen display, **so that** business logic is centralized and consistent.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] All frontend applications consume the same REST API
- [ ] API enforces role-based access control (Platform Admin, Brand Admin, Shop Manager, Counter Staff, Kitchen Staff, Floor Staff, Customer)
- [ ] API routes are tenant-aware (brand-scoped and shop-scoped endpoints)
- [ ] API returns appropriate HTTP status codes and error messages

---

### US-FP-070: Database-per-brand provisioning
**As a** System, **I want** to automatically provision a new database when a brand is created, **so that** data isolation is enforced from the start.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] Creating a brand via the API triggers creation of a new isolated database
- [ ] The platform database maintains a registry mapping brand IDs to their database connection strings
- [ ] API dynamically routes queries to the correct brand database based on the request context
- [ ] Database schema migrations are applied to all brand databases consistently

---

### US-FP-071: Storefront landing & shop selection
**As a** Customer (guest or registered), **I want** to arrive on a brand's storefront and find or select a shop, **so that** I can reach a shop's menu and order without already knowing a shop's URL.

**Priority:** Must Have

**Acceptance Criteria:**
- [ ] `/{brandSlug}/{lang}/shops` lists the brand's active shops (name, address, open/closed status) to choose from; the landing (`/{brandSlug}/{lang}`) routes here
- [ ] Shops have a unique, brand-scoped slug (backend), plus a public endpoint to list a brand's active shops and resolve a shop by slug
- [ ] Selecting a shop navigates to `/{brandSlug}/{lang}/{shopSlug}/menu`; the order flow is nested under the shop slug (`…/{shopSlug}/checkout`, `…/{shopSlug}/order/{orderId}`, `…/{shopSlug}/order/{orderId}/track`)
- [ ] Shop context is read from the route slug — the localStorage/sessionStorage shopId-recovery hacks are removed
- [ ] If the brand has exactly one active shop, the customer can be taken straight to its menu
- [ ] Closed / inactive shops are clearly indicated and cannot start an order
- [ ] Deep links to `…/{shopSlug}/menu` work directly

> Added 2026-06-03 (GitHub issue #127). Shop selection + shop-scoped routing; closes the storefront entry-point gap blocking US-FP-016 / 017 / 038 / 058 / 063 from being user-reachable.

---
