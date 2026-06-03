import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '@/auth/useAuth';
import { LanguageSelector } from '@features/storefront/components/LanguageSelector';
import { PosOrderProvider, useOrderState } from '../context/PosOrderContext';
import { PosMenuGrid } from '../components/PosMenuGrid';
import { PosOrderPanel } from '../components/PosOrderPanel';
import { useCreateInStoreOrder } from '../hooks/useCreateInStoreOrder';

// ---------------------------------------------------------------------------
// Inner dashboard — must be inside PosOrderProvider
// ---------------------------------------------------------------------------

interface PosDashboardInnerProps {
  brandSlug: string;
  shopId: string;
}

function PosDashboardInner({ brandSlug, shopId }: PosDashboardInnerProps) {
  const { t } = useTranslation('common');
  const { user } = useAuth();
  const { state } = useOrderState();
  const { lang, brandSlug: paramBrandSlug } = useParams<{ lang: string; brandSlug: string }>();
  const navigate = useNavigate();

  const mutation = useCreateInStoreOrder(brandSlug, shopId);

  // Client-side guard: block submit when order is empty or EatIn without table number
  const isEatInMissingTable =
    state.orderType === 'EatIn' && !state.tableNumber;
  const canSubmit =
    state.items.length > 0 && !isEatInMissingTable && !mutation.isPending;

  async function handlePlaceOrder() {
    if (!canSubmit) return;
    try {
      const order = await mutation.mutateAsync({});
      void navigate(`/${paramBrandSlug}/${lang}/pos/confirmation/${order.orderNumber}`);
    } catch {
      // error is exposed via mutation.isError
    }
  }

  // Derive a friendly brand display name from the slug (capitalise first letter)
  const brandDisplayName =
    brandSlug.charAt(0).toUpperCase() + brandSlug.slice(1);

  return (
    <div
      style={{
        display: 'grid',
        gridTemplateRows: 'auto 1fr',
        gridTemplateColumns: '1fr 22rem',
        height: '100dvh',
        overflow: 'hidden',
      }}
    >
      {/* Header — spans full width */}
      <header
        style={{
          gridColumn: '1 / -1',
          padding: '0.875rem 1.25rem',
          borderBottom: '1px solid #e5e7eb',
          background: '#fff',
          display: 'flex',
          alignItems: 'center',
          gap: '1rem',
        }}
      >
        <span style={{ fontWeight: 700, fontSize: '1.0625rem', color: '#111827' }}>
          {brandDisplayName} — POS
        </span>
        {user && (
          <span style={{ fontSize: '0.875rem', color: '#6b7280' }}>
            {user.displayName}{' '}
            <span style={{ color: '#9ca3af' }}>
              ({user.roles.join(', ')})
            </span>
          </span>
        )}
        <div style={{ marginLeft: 'auto' }}>
          <LanguageSelector />
        </div>
      </header>

      {/* Menu grid — left column */}
      <div style={{ overflowY: 'auto', background: '#fff' }}>
        <PosMenuGrid brandSlug={brandSlug} />
      </div>

      {/* Order panel + submit — right column */}
      <div
        style={{
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
          borderLeft: '1px solid #e5e7eb',
        }}
      >
        <div style={{ flex: 1, overflow: 'hidden' }}>
          <PosOrderPanel />
        </div>

        {/* Validation feedback */}
        {state.items.length > 0 && isEatInMissingTable && (
          <div
            role="alert"
            style={{
              margin: '0 1.25rem',
              padding: '0.625rem 0.875rem',
              borderRadius: '0.375rem',
              background: '#fef2f2',
              border: '1px solid #fecaca',
              color: '#991b1b',
              fontSize: '0.875rem',
            }}
          >
            {t('pos.order.tableNumber')} {t('error')}
          </div>
        )}

        {mutation.isError && (
          <div
            role="alert"
            style={{
              margin: '0 1.25rem',
              padding: '0.625rem 0.875rem',
              borderRadius: '0.375rem',
              background: '#fef2f2',
              border: '1px solid #fecaca',
              color: '#991b1b',
              fontSize: '0.875rem',
            }}
          >
            {t('pos.error.submit')}
          </div>
        )}

        {/* Place Order button */}
        <div style={{ padding: '1rem 1.25rem', background: '#fff', borderTop: '1px solid #e5e7eb' }}>
          <button
            type="button"
            onClick={() => { void handlePlaceOrder(); }}
            disabled={!canSubmit}
            aria-disabled={!canSubmit}
            style={{
              width: '100%',
              padding: '1rem',
              borderRadius: '0.5rem',
              border: 'none',
              background: canSubmit ? 'var(--brand-color-primary, #111827)' : '#9ca3af',
              color: '#fff',
              fontWeight: 700,
              fontSize: '1.0625rem',
              cursor: canSubmit ? 'pointer' : 'not-allowed',
              minHeight: '3.5rem',
            }}
          >
            {mutation.isPending ? '…' : t('pos.order.submit')}
          </button>
        </div>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// POS Dashboard — wraps inner component in the PosOrderProvider
// ---------------------------------------------------------------------------

/**
 * Main POS page for placing in-store orders.
 *
 * Reads brandSlug and shopId from route params.
 * shopId is assumed to be provided in the URL; in production it will come from
 * a shop-selection step or the staff's assigned shop.
 */
export function PosDashboard() {
  const { brandSlug, shopId } = useParams<{ brandSlug: string; shopId: string }>();
  const resolvedBrandSlug = brandSlug ?? 'frietjes';
  // shopId is optional in the route for now; fall back to 'shop-1' for dev
  const resolvedShopId = shopId ?? 'shop-1';

  return (
    <PosOrderProvider>
      <PosDashboardInner brandSlug={resolvedBrandSlug} shopId={resolvedShopId} />
    </PosOrderProvider>
  );
}
