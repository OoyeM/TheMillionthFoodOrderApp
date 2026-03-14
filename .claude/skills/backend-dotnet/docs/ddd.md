# Domain-Driven Design

## Purpose
Rich domain modeling for the restaurant ordering platform. Business logic lives in the domain layer, not in endpoints or infrastructure.

## Bounded Contexts

| Context | Core Responsibility | Key Aggregates |
|---------|-------------------|----------------|
| **Tenant Management** | Platform > Brand > Shop hierarchy, provisioning | Brand, Shop |
| **Catalog** | Product definitions, modifiers, combos, categories, allergens | Product, Category, ModifierGroup |
| **Ordering** | Order placement, lifecycle, time slots, notifications | Order, OrderItem, TimeSlot |
| **Pricing** | Base prices, shop overrides, VAT calculation, discounts | PricePolicy, DiscountCode |
| **Identity** | Staff auth, customer accounts, roles, SSO | User, Role |
| **Fulfillment** | Kitchen display, ticket printing, order status transitions | FulfillmentTicket, OrderStatus |
| **Scheduling** | Staff shifts, availability, swap requests | Shift, StaffAvailability |
| **Loyalty** | Stamp system, points, redemption | LoyaltyAccount, StampCard |
| **Analytics** | Sales reporting, product performance, peak hours | (read models, no aggregates) |

## Aggregate Design Rules

- Aggregates are consistency boundaries — only one aggregate per transaction
- Reference other aggregates by ID, never by direct object reference
- Keep aggregates small — prefer more smaller aggregates over fewer large ones
- Aggregate roots are the only entry point — external code never reaches into child entities

## Entities vs Value Objects

**Entities** — have identity, tracked over time:
- Product, Order, Shop, Brand, User, Shift

**Value Objects** — defined by attributes, immutable, no identity:
- Money (amount + currency), Address, TimeSlotWindow, AllergenInfo, VatRate, Translation

## Domain Events

Events signal that something meaningful happened in the domain. Use for cross-context communication.

Examples:
- `OrderPlaced` → triggers fulfillment ticket, notifications, stock decrement
- `ProductApprovalRequested` → shop submitted custom product for brand review
- `StockDepleted` → product auto-disabled on storefront
- `OrderStatusChanged` → customer notification, kitchen display update

## Approval Workflow Pattern

Shop-level customizations (custom products, price overrides) follow a request/approval flow:
1. Shop submits request → creates a pending entity
2. Brand admin reviews → `Approved`, `Rejected`, or `ReturnedForRevision`
3. On approval → entity becomes active in the shop's catalog

## Multi-Tenant Considerations

- Each Brand has its own database — complete data isolation
- Platform-level data (brand registry) lives in a shared platform database
- Tenant resolution happens at the BFF/API boundary, not in the domain
- Domain models are tenant-unaware — they don't know about multi-tenancy

## Belgian Domain Constraints

- **14 EU allergens** must be tracked per product (legally required)
- **VAT**: 6% takeaway, 21% eat-in — determined by order type, not product
- **Languages**: NL, FR, DE — all user-facing text needs Translation value objects

## Messaging

- MassTransit for async messaging between bounded contexts
- In-memory transport for local dev, RabbitMQ or Azure Service Bus for production
- Sagas/state machines for complex workflows (e.g., order lifecycle)
- Domain events published via MassTransit, not MediatR

## Plugins for DDD Patterns

For general .NET DDD implementation patterns (aggregates, handlers, repositories, Unit of Work), use:
- `dotnet-claude-code-skills` plugin (nesbo) — `ddd-dotnet` skill
- `dotnet-claude-kit` plugin (codewithmukesh) — `dotnet-architect` agent
