import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useActiveOrders } from '../hooks/useActiveOrders';
import { KitchenOrderCard } from '../components/KitchenOrderCard';
import type { ConnectionStatus } from '@api/useSignalR';

const connectionColor: Record<ConnectionStatus, string> = {
  connected: '#16a34a',
  connecting: '#d97706',
  reconnecting: '#d97706',
  disconnected: '#dc2626',
};

export function KitchenDisplay() {
  const { t } = useTranslation('common');
  const { brandSlug, shopId } = useParams<{
    brandSlug: string;
    lang: string;
    shopId: string;
  }>();

  const resolvedBrand = brandSlug ?? '';
  const resolvedShop = shopId ?? '';

  const { orders, isLoading, isError, connectionStatus } = useActiveOrders(
    resolvedBrand,
    resolvedShop,
  );

  return (
    <main
      style={{
        maxWidth: '90rem',
        margin: '0 auto',
        padding: '1rem',
        display: 'flex',
        flexDirection: 'column',
        gap: '1rem',
      }}
    >
      <header
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          gap: '1rem',
          flexWrap: 'wrap',
        }}
      >
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800, margin: 0 }}>
          {t('pos.kitchen.title')}
        </h1>
        <div
          data-testid="kitchen-connection-status"
          style={{
            display: 'inline-flex',
            alignItems: 'center',
            gap: '0.5rem',
            padding: '0.375rem 0.75rem',
            background: '#f3f4f6',
            borderRadius: '999px',
            fontSize: '0.8125rem',
            fontWeight: 600,
            color: '#374151',
          }}
        >
          <span
            style={{
              width: '0.625rem',
              height: '0.625rem',
              borderRadius: '50%',
              background: connectionColor[connectionStatus],
              display: 'inline-block',
            }}
          />
          {t(`pos.kitchen.connection.${connectionStatus}`)}
        </div>
      </header>

      {isLoading && (
        <p style={{ color: '#6b7280', margin: 0 }} data-testid="kitchen-loading">
          {t('loading')}
        </p>
      )}

      {isError && (
        <p style={{ color: '#dc2626', margin: 0 }} data-testid="kitchen-error">
          {t('error')}
        </p>
      )}

      {!isLoading && !isError && orders !== undefined && orders.length === 0 && (
        <p
          style={{
            color: '#6b7280',
            margin: 0,
            padding: '2rem',
            textAlign: 'center',
            background: '#f9fafb',
            borderRadius: '0.75rem',
          }}
          data-testid="kitchen-empty"
        >
          {t('pos.kitchen.empty')}
        </p>
      )}

      {orders !== undefined && orders.length > 0 && (
        <section
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(18rem, 1fr))',
            gap: '0.75rem',
          }}
          data-testid="kitchen-order-grid"
        >
          {orders.map((order) => (
            <KitchenOrderCard key={order.id} order={order} />
          ))}
        </section>
      )}
    </main>
  );
}
