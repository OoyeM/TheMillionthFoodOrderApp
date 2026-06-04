import { http, HttpResponse } from 'msw';

/**
 * MSW v2 request handlers covering all API routes used by src/api/* modules.
 *
 * These handlers provide sensible defaults that return valid fixture data.
 * Individual tests can override specific handlers via server.use(...) to
 * simulate error conditions (401, 403, 404, 500).
 */
export const handlers = [
  // ── BFF Auth endpoints (/bff/*) ──────────────────────────────────────────

  http.get('/bff/user', () =>
    HttpResponse.json({
      isAuthenticated: true,
      userId: 'user-1',
      displayName: 'Test User',
      email: 'test@example.com',
      roles: ['brand-admin'],
      brandSlug: 'frietjes',
      firstName: 'Test',
      lastName: 'User',
      phoneNumber: '+32470000001',
    }),
  ),

  http.post('/bff/logout', () => new HttpResponse(null, { status: 200 })),

  http.post('/bff/session/keepalive', () => new HttpResponse(null, { status: 200 })),

  // ── Brands (/api/brands) ─────────────────────────────────────────────────

  http.get('/api/brands', () =>
    HttpResponse.json([
      {
        id: 'brand-1',
        slug: 'frietjes',
        name: 'Frietjes?',
        contactEmail: 'admin@frietjes.be',
        contactPhone: null,
        isActive: true,
        databaseName: 'BrandDb_frietjes',
        staffAuthMethod: 'EmailPassword',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
      },
    ]),
  ),

  http.get('/api/brands/:id', ({ params }) =>
    HttpResponse.json({
      id: params.id,
      slug: 'frietjes',
      name: 'Frietjes?',
      contactEmail: 'admin@frietjes.be',
      contactPhone: null,
      isActive: true,
      databaseName: 'BrandDb_frietjes',
      staffAuthMethod: 'EmailPassword',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    }),
  ),

  http.post('/api/brands', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json(
      {
        id: 'brand-new',
        slug: body.slug,
        name: body.name,
        contactEmail: body.contactEmail,
        contactPhone: null,
        isActive: true,
        databaseName: `BrandDb_${String(body.slug)}`,
        staffAuthMethod: 'EmailPassword',
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
      },
      { status: 201 },
    );
  }),

  http.put('/api/brands/:id', async ({ params, request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json({
      id: params.id,
      slug: 'frietjes',
      name: body.name,
      contactEmail: body.contactEmail,
      contactPhone: null,
      isActive: true,
      databaseName: 'BrandDb_frietjes',
      staffAuthMethod: 'EmailPassword',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-06-01T00:00:00Z',
    });
  }),

  http.post('/api/brands/:id/deactivate', () => new HttpResponse(null, { status: 200 })),
  http.post('/api/brands/:id/activate', () => new HttpResponse(null, { status: 200 })),

  http.put('/api/brands/:slug/staff-auth', () =>
    HttpResponse.json({
      id: 'brand-1',
      slug: 'frietjes',
      name: 'Frietjes?',
      contactEmail: 'admin@frietjes.be',
      contactPhone: null,
      isActive: true,
      databaseName: 'BrandDb_frietjes',
      staffAuthMethod: 'GoogleSso',
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-06-01T00:00:00Z',
    }),
  ),

  // ── Brand Settings (/api/brands/:slug/settings) ──────────────────────────

  http.get('/api/brands/:slug/settings', () =>
    HttpResponse.json({
      id: 'settings-1',
      defaultLanguage: 'nl',
      timezone: 'Europe/Brussels',
      currency: 'EUR',
      logoUrl: null,
      customDomain: null,
      colors: null,
      typography: null,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    }),
  ),

  http.put('/api/brands/:slug/settings/theming', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json({
      id: 'settings-1',
      defaultLanguage: 'nl',
      timezone: 'Europe/Brussels',
      currency: 'EUR',
      logoUrl: null,
      customDomain: body.customDomain ?? null,
      colors: body.colors ?? null,
      typography: body.typography ?? null,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-06-01T00:00:00Z',
    });
  }),

  http.post('/api/brands/:slug/settings/logo', () =>
    HttpResponse.json({ logoUrl: 'https://cdn.example.com/logo.png' }),
  ),

  http.get('/api/brands/:slug/theme', () =>
    HttpResponse.json({
      logoUrl: null,
      customDomain: null,
      primaryColor: '#2563eb',
      secondaryColor: '#64748b',
      accentColor: '#f59e0b',
      headingFontFamily: 'Inter',
      bodyFontFamily: 'System Default',
    }),
  ),

  // ── Brand Staff (/api/brands/:slug/staff) ───────────────────────────────

  http.get('/api/brands/:slug/staff', () =>
    HttpResponse.json([
      {
        id: 'staff-1',
        email: 'staff@frietjes.be',
        displayName: 'Staff Member',
        roleId: 'role-1',
        role: 0,
        shopId: null,
        shopName: null,
        createdAt: '2024-01-01T00:00:00Z',
      },
    ]),
  ),

  http.get('/api/brands/:slug/shops/:shopId/staff', () =>
    HttpResponse.json([]),
  ),

  http.post('/api/brands/:slug/staff', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json(
      {
        id: 'staff-new',
        email: body.email,
        displayName: body.displayName,
        roleId: 'role-new',
        role: body.role,
        shopId: body.shopId ?? null,
        shopName: null,
        createdAt: '2024-06-01T00:00:00Z',
      },
      { status: 201 },
    );
  }),

  http.post('/api/brands/:slug/staff/:roleId/deactivate', () =>
    new HttpResponse(null, { status: 200 }),
  ),

  // ── Shops (/api/brands/:slug/shops) ─────────────────────────────────────

  http.get('/api/brands/:slug/shops', () =>
    HttpResponse.json([
      {
        id: 'shop-1',
        name: 'Gent Centrum',
        slug: 'gent-centrum',
        address: {
          street: 'Veldstraat',
          number: '1',
          city: 'Gent',
          postalCode: '9000',
          country: 'BE',
        },
        contactEmail: 'gent@frietjes.be',
        contactPhone: null,
        isActive: true,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
      },
    ]),
  ),

  // Active shops for the storefront chooser + POS shop-config lookup (US-FP-071 / US-FP-066).
  // Must precede the /shops/:shopId handler so "active" is not matched as a shopId.
  http.get('/api/brands/:slug/shops/active', () =>
    HttpResponse.json([
      {
        id: 'shop-1',
        name: 'Gent Centrum',
        slug: 'gent-centrum',
        address: {
          street: 'Veldstraat',
          number: '1',
          city: 'Gent',
          postalCode: '9000',
          country: 'BE',
        },
        isOpen: true,
        eatIn: { isEnabled: true, requiresTableNumber: true },
      },
    ]),
  ),

  http.get('/api/brands/:slug/shops/:shopId', ({ params }) =>
    HttpResponse.json({
      id: params.shopId,
      name: 'Gent Centrum',
      slug: 'gent-centrum',
      address: {
        street: 'Veldstraat',
        number: '1',
        city: 'Gent',
        postalCode: '9000',
        country: 'BE',
      },
      contactEmail: 'gent@frietjes.be',
      contactPhone: null,
      isActive: true,
      eatIn: { isEnabled: true, requiresTableNumber: true },
      timeSlotOrdering: { isEnabled: false, intervalMinutes: null, maxOrdersPerInterval: null },
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    }),
  ),

  http.post('/api/brands/:slug/shops', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json(
      {
        id: 'shop-new',
        name: body.name,
        slug: body.slug,
        address: body.address,
        contactEmail: body.contactEmail,
        contactPhone: body.contactPhone ?? null,
        isActive: true,
        createdAt: '2024-06-01T00:00:00Z',
        updatedAt: '2024-06-01T00:00:00Z',
      },
      { status: 201 },
    );
  }),

  http.put('/api/brands/:slug/shops/:shopId', async ({ params, request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json({
      id: params.shopId,
      name: body.name,
      slug: 'gent-centrum',
      address: body.address,
      contactEmail: body.contactEmail,
      contactPhone: body.contactPhone ?? null,
      isActive: true,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-06-01T00:00:00Z',
    });
  }),

  http.post('/api/brands/:slug/shops/:shopId/deactivate', () =>
    new HttpResponse(null, { status: 200 }),
  ),

  http.post('/api/brands/:slug/shops/:shopId/activate', () =>
    new HttpResponse(null, { status: 200 }),
  ),

  // ── Opening Hours (/api/brands/:slug/shops/:shopId/opening-hours) ────────

  http.get('/api/brands/:slug/shops/:shopId/opening-hours', () =>
    HttpResponse.json({
      timeBlocks: [
        { id: 'tb-1', dayOfWeek: 1, openTime: '09:00', closeTime: '18:00' },
      ],
    }),
  ),

  http.put('/api/brands/:slug/shops/:shopId/opening-hours', async ({ request }) => {
    const body = await request.json() as { timeBlocks: unknown[] };
    return HttpResponse.json({
      timeBlocks: body.timeBlocks.map((tb, i) => ({ id: `tb-${String(i)}`, ...tb as object })),
    });
  }),

  http.get('/api/brands/:slug/shops/:shopId/status', () =>
    HttpResponse.json({
      isOpen: true,
      nextOpeningTime: null,
      timeZoneId: 'Europe/Brussels',
    }),
  ),

  // ── Order Lifecycle (/api/brands/:slug/shops/:shopId/order-lifecycle) ────

  http.get('/api/brands/:slug/shops/:shopId/order-lifecycle', ({ params }) =>
    HttpResponse.json({
      shopId: params.shopId,
      statuses: [
        {
          id: 'status-1',
          name: 'New',
          systemKey: 'new',
          sortOrder: 1,
          isEnabled: true,
          isTerminal: false,
          colorHex: '#3b82f6',
        },
      ],
      transitions: [],
    }),
  ),

  http.put('/api/brands/:slug/shops/:shopId/order-lifecycle', async ({ params, request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json({
      shopId: params.shopId,
      statuses: body.statuses,
      transitions: body.transitions,
    });
  }),

  http.post('/api/brands/:slug/shops/:shopId/order-lifecycle/reset', ({ params }) =>
    HttpResponse.json({
      shopId: params.shopId,
      statuses: [],
      transitions: [],
    }),
  ),

  // ── Menu Categories (/api/brands/:slug/menu-categories) ─────────────────

  http.get('/api/brands/:slug/menu-categories', () =>
    HttpResponse.json([
      {
        id: 'cat-1',
        name: 'Frietjes',
        sortOrder: 1,
        imageUrl: null,
        productCount: 3,
        createdAt: '2024-01-01T00:00:00Z',
      },
    ]),
  ),

  http.get('/api/brands/:slug/menu-categories/:id', ({ params }) =>
    HttpResponse.json({
      id: params.id,
      sortOrder: 1,
      imageUrl: null,
      productCount: 3,
      translations: [{ languageCode: 'nl', name: 'Frietjes' }],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    }),
  ),

  http.post('/api/brands/:slug/menu-categories', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json(
      {
        id: 'cat-new',
        sortOrder: body.sortOrder,
        imageUrl: body.imageUrl ?? null,
        productCount: 0,
        translations: body.translations,
        createdAt: '2024-06-01T00:00:00Z',
        updatedAt: '2024-06-01T00:00:00Z',
      },
      { status: 201 },
    );
  }),

  http.put('/api/brands/:slug/menu-categories/:id', async ({ params, request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json({
      id: params.id,
      sortOrder: body.sortOrder,
      imageUrl: body.imageUrl ?? null,
      productCount: 3,
      translations: body.translations,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-06-01T00:00:00Z',
    });
  }),

  http.delete('/api/brands/:slug/menu-categories/:id', () =>
    new HttpResponse(null, { status: 204 }),
  ),

  http.patch('/api/brands/:slug/menu-categories/:id/sort-order', () =>
    new HttpResponse(null, { status: 200 }),
  ),

  http.post('/api/brands/:slug/menu-categories/assign-product', () =>
    new HttpResponse(null, { status: 200 }),
  ),

  http.get('/api/brands/:slug/menu-categories/:id/products', () =>
    HttpResponse.json([]),
  ),

  http.put('/api/brands/:slug/menu-categories/:id/products/order', () =>
    new HttpResponse(null, { status: 200 }),
  ),

  // ── Modifier Groups (/api/brands/:slug/modifier-groups) ──────────────────

  http.get('/api/brands/:slug/modifier-groups', () =>
    HttpResponse.json([
      {
        id: 'mg-1',
        name: 'Sauzen',
        modifierCount: 3,
        productCount: 5,
        createdAt: '2024-01-01T00:00:00Z',
      },
    ]),
  ),

  http.get('/api/brands/:slug/modifier-groups/:id', ({ params }) =>
    HttpResponse.json({
      id: params.id,
      translations: [{ languageCode: 'nl', name: 'Sauzen' }],
      modifiers: [
        {
          id: 'mod-1',
          priceAdjustment: 0,
          sortOrder: 1,
          translations: [{ languageCode: 'nl', name: 'Mayonaise' }],
        },
      ],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    }),
  ),

  http.post('/api/brands/:slug/modifier-groups', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json(
      {
        id: 'mg-new',
        translations: body.translations,
        modifiers: body.modifiers,
        createdAt: '2024-06-01T00:00:00Z',
        updatedAt: '2024-06-01T00:00:00Z',
      },
      { status: 201 },
    );
  }),

  http.put('/api/brands/:slug/modifier-groups/:id', async ({ params, request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json({
      id: params.id,
      translations: body.translations,
      modifiers: body.modifiers,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-06-01T00:00:00Z',
    });
  }),

  http.delete('/api/brands/:slug/modifier-groups/:id', () =>
    new HttpResponse(null, { status: 204 }),
  ),

  http.get('/api/brands/:slug/products/:productId/modifier-groups', () =>
    HttpResponse.json([
      {
        modifierGroupId: 'mg-1',
        name: 'Sauzen',
        sortOrder: 1,
        modifiers: [],
      },
    ]),
  ),

  http.put('/api/brands/:slug/products/:productId/modifier-groups', async ({ request }) => {
    // Mirror the backend contract: the body must carry an `assignments` array
    // (SetProductModifierGroupsApiRequest.Assignments), else 400 — guards against
    // the field-name regression where the client sent `modifierGroups`.
    const body = (await request.json().catch(() => null)) as { assignments?: unknown } | null;
    if (!body || !Array.isArray(body.assignments)) {
      return HttpResponse.json(
        { assignments: ['Assignments list is required.'] },
        { status: 400 },
      );
    }
    return new HttpResponse(null, { status: 200 });
  }),

  // ── Products (/api/brands/:slug/products) ────────────────────────────────

  http.get('/api/brands/:slug/products', () =>
    HttpResponse.json([
      {
        id: 'prod-1',
        productType: 'Simple',
        name: 'Kleine friet',
        basePrice: { amount: 3.5, currency: 'EUR' },
        imageUrl: null,
        menuCategoryId: 'cat-1',
        sortOrderInCategory: 1,
        allergens: [],
        dietaryTags: [],
        createdAt: '2024-01-01T00:00:00Z',
      },
    ]),
  ),

  http.get('/api/brands/:slug/products/:id', ({ params }) =>
    HttpResponse.json({
      id: params.id,
      productType: 'Simple',
      basePrice: { amount: 3.5, currency: 'EUR' },
      imageUrl: null,
      menuCategoryId: 'cat-1',
      sortOrderInCategory: 1,
      translations: [{ languageCode: 'nl', name: 'Kleine friet', description: null }],
      allergens: [],
      dietaryTags: [],
      comboItems: null,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    }),
  ),

  http.post('/api/brands/:slug/products', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json(
      {
        id: 'prod-new',
        productType: 'Simple',
        basePrice: { amount: body.basePrice, currency: 'EUR' },
        imageUrl: body.imageUrl ?? null,
        menuCategoryId: null,
        sortOrderInCategory: 0,
        translations: body.translations,
        allergens: body.allergens ?? [],
        dietaryTags: body.dietaryTags ?? [],
        comboItems: null,
        createdAt: '2024-06-01T00:00:00Z',
        updatedAt: '2024-06-01T00:00:00Z',
      },
      { status: 201 },
    );
  }),

  http.put('/api/brands/:slug/products/:id', async ({ params, request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json({
      id: params.id,
      productType: 'Simple',
      basePrice: { amount: body.basePrice, currency: 'EUR' },
      imageUrl: body.imageUrl ?? null,
      menuCategoryId: null,
      sortOrderInCategory: 0,
      translations: body.translations,
      allergens: body.allergens ?? [],
      dietaryTags: body.dietaryTags ?? [],
      comboItems: null,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-06-01T00:00:00Z',
    });
  }),

  http.delete('/api/brands/:slug/products/:id', () =>
    new HttpResponse(null, { status: 204 }),
  ),

  http.post('/api/brands/:slug/combo-products', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json(
      {
        id: 'combo-new',
        productType: 'Combo',
        basePrice: { amount: body.basePrice, currency: 'EUR' },
        imageUrl: body.imageUrl ?? null,
        menuCategoryId: null,
        sortOrderInCategory: 0,
        translations: body.translations,
        allergens: [],
        dietaryTags: [],
        comboItems: (body.componentProductIds as string[]).map((id, i) => ({
          componentProductId: id,
          name: `Product ${String(i + 1)}`,
          sortOrder: i,
        })),
        createdAt: '2024-06-01T00:00:00Z',
        updatedAt: '2024-06-01T00:00:00Z',
      },
      { status: 201 },
    );
  }),

  http.put('/api/brands/:slug/combo-products/:id', async ({ params, request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json({
      id: params.id,
      productType: 'Combo',
      basePrice: { amount: body.basePrice, currency: 'EUR' },
      imageUrl: body.imageUrl ?? null,
      menuCategoryId: null,
      sortOrderInCategory: 0,
      translations: body.translations,
      allergens: [],
      dietaryTags: [],
      comboItems: (body.componentProductIds as string[]).map((id, i) => ({
        componentProductId: id,
        name: `Product ${String(i + 1)}`,
        sortOrder: i,
      })),
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-06-01T00:00:00Z',
    });
  }),

  // ── Platform Admins (/api/platform-admins) ────────────────────────────────

  http.get('/api/platform-admins', () =>
    HttpResponse.json([
      {
        id: 'pa-1',
        email: 'admin@platform.dev',
        displayName: 'Platform Admin',
        isPlatformAdmin: true,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
      },
    ]),
  ),

  http.post('/api/platform-admins', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json(
      {
        id: 'pa-new',
        email: body.email,
        displayName: body.displayName,
        isPlatformAdmin: true,
        createdAt: '2024-06-01T00:00:00Z',
        updatedAt: '2024-06-01T00:00:00Z',
      },
      { status: 201 },
    );
  }),

  http.post('/api/platform-admins/:id/deactivate', () =>
    new HttpResponse(null, { status: 200 }),
  ),

  // ── Online Orders (/api/brands/:slug/shops/:shopId/orders) ──────────────

  http.post('/api/brands/:slug/shops/:shopId/orders', async ({ params, request }) => {
    const body = await request.json() as {
      orderType: string;
      paymentMethod: string;
      customerFirstName?: string | null;
      customerLastName?: string | null;
      customerEmail?: string | null;
      customerPhone?: string | null;
      languageCode?: string;
      items: { productId: string; quantity: number; selectedModifierIds: string[] }[];
    };

    const validOrderTypes = ['Pickup', 'EatIn', 'Delivery'];
    if (!validOrderTypes.includes(body.orderType)) {
      return HttpResponse.json({ error: `Invalid orderType: ${body.orderType}` }, { status: 400 });
    }
    if (!Array.isArray(body.items) || body.items.length === 0) {
      return HttpResponse.json({ error: 'items must be a non-empty array' }, { status: 400 });
    }

    const nameParts = [body.customerFirstName, body.customerLastName].filter(Boolean);
    const customerName = nameParts.length > 0 ? nameParts.join(' ') : null;

    return HttpResponse.json(
      {
        id: 'order-1',
        orderNumber: 'ORD-001',
        shopId: params.shopId,
        brandSlug: params.slug,
        orderType: body.orderType,
        statusName: 'New',
        customerName,
        customerFirstName: body.customerFirstName ?? null,
        customerLastName: body.customerLastName ?? null,
        languageCode: body.languageCode ?? null,
        customerEmail: body.customerEmail ?? null,
        customerPhone: body.customerPhone ?? null,
        items: body.items.map((item) => ({
          productId: item.productId,
          productName: 'Kleine friet',
          quantity: item.quantity,
          unitGrossPrice: 3.5,
          unitNetPrice: 3.3,
          unitVatAmount: 0.2,
          lineTotal: item.quantity * 3.5,
          selectedModifiers: item.selectedModifierIds.map((id) => ({
            modifierId: id,
            modifierName: 'Mayonaise',
            priceAdjustment: 0,
          })),
        })),
        vatRatePercent: body.orderType === 'EatIn' ? 21 : 6,
        subtotalGross: body.items.reduce(
          (sum: number, item) => sum + item.quantity * 3.5,
          0,
        ),
        totalVatAmount: 0.2,
        totalNet: 3.3,
        totalGross: body.items.reduce(
          (sum: number, item) => sum + item.quantity * 3.5,
          0,
        ),
        createdAt: '2024-06-01T10:00:00Z',
        paymentMethod: body.paymentMethod,
        shopName: 'Gent Centrum',
        shopVatNumber: null,
        shopAddressLine: null,
      },
      { status: 201 },
    );
  }),

  // ── In-store Orders (/api/brands/:slug/shops/:shopId/orders/in-store) ────

  http.post('/api/brands/:slug/shops/:shopId/orders/in-store', async ({ params, request }) => {
    const body = await request.json() as {
      orderType: string;
      paymentMethod: string;
      tableNumber?: number;
      customerFirstName?: string;
      customerLastName?: string;
      items: { productId: string; quantity: number; selectedModifierIds: string[] }[];
    };

    // Minimal server-side validation: reject obviously malformed payloads in tests
    const validOrderTypes = ['Pickup', 'EatIn', 'Delivery'];
    if (!validOrderTypes.includes(body.orderType)) {
      return HttpResponse.json({ error: `Invalid orderType: ${body.orderType}` }, { status: 400 });
    }
    if (!Array.isArray(body.items) || body.items.length === 0) {
      return HttpResponse.json({ error: 'items must be a non-empty array' }, { status: 400 });
    }
    if (!body.paymentMethod) {
      return HttpResponse.json({ error: 'paymentMethod is required' }, { status: 400 });
    }

    return HttpResponse.json(
      {
        id: 'instore-order-1',
        orderNumber: 'ORD-999',
        shopId: params.shopId,
        brandSlug: params.slug,
        orderType: body.orderType,
        statusName: 'New',
        customerName: [body.customerFirstName, body.customerLastName].filter(Boolean).join(' ') || null,
        customerFirstName: body.customerFirstName ?? null,
        customerLastName: body.customerLastName ?? null,
        items: body.items.map((item) => ({
          productId: item.productId,
          productName: 'Kleine friet',
          quantity: item.quantity,
          unitGrossPrice: 3.5,
          unitNetPrice: 3.3,
          unitVatAmount: 0.2,
          lineTotal: item.quantity * 3.5,
          selectedModifiers: item.selectedModifierIds.map((id) => ({
            modifierId: id,
            modifierName: 'Mayonaise',
            priceAdjustment: 0,
          })),
        })),
        vatRatePercent: body.orderType === 'EatIn' ? 21 : 6,
        subtotalGross: body.items.reduce(
          (sum: number, item) => sum + item.quantity * 3.5,
          0,
        ),
        totalVatAmount: 0.2,
        totalNet: 3.3,
        totalGross: body.items.reduce(
          (sum: number, item) => sum + item.quantity * 3.5,
          0,
        ),
        createdAt: '2024-06-01T10:00:00Z',
        paymentMethod: body.paymentMethod,
        tableNumber: body.tableNumber,
        createdByStaffId: 'staff-1',
      },
      { status: 201 },
    );
  }),

  // ── Tax Configuration (/api/brands/:slug/tax-configuration) ─────────────

  http.get('/api/brands/:slug/tax-configuration', () =>
    HttpResponse.json({
      id: 'tax-1',
      vatRates: [
        { consumptionMode: 'Takeaway', ratePercentage: 6 },
        { consumptionMode: 'EatIn', ratePercentage: 21 },
      ],
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-01-01T00:00:00Z',
    }),
  ),

  http.put('/api/brands/:slug/tax-configuration', async ({ request }) => {
    const body = await request.json() as Record<string, unknown>;
    return HttpResponse.json({
      id: 'tax-1',
      vatRates: body.vatRates,
      createdAt: '2024-01-01T00:00:00Z',
      updatedAt: '2024-06-01T00:00:00Z',
    });
  }),

  http.post('/api/brands/:slug/tax-configuration/calculate', async ({ request }) => {
    const body = await request.json() as { grossAmount: number; consumptionMode: string };
    const rate = body.consumptionMode === 'Takeaway' ? 6 : 21;
    const vatAmount = parseFloat(((body.grossAmount * rate) / (100 + rate)).toFixed(2));
    return HttpResponse.json({
      netAmount: parseFloat((body.grossAmount - vatAmount).toFixed(2)),
      vatAmount,
      grossAmount: body.grossAmount,
      vatRatePercentage: rate,
    });
  }),
];
