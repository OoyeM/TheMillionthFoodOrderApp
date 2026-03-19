import { useQuery } from '@tanstack/react-query';
import { openingHoursApi } from '@api/openingHours';

interface ShopStatusBadgeProps {
  brandSlug: string;
  shopId: string;
}

/**
 * Displays a real-time open/closed badge for a shop.
 * Auto-refreshes every 60 seconds so the status stays current without a page reload.
 */
export function ShopStatusBadge({ brandSlug, shopId }: ShopStatusBadgeProps) {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['shopStatus', brandSlug, shopId],
    queryFn: () => openingHoursApi.getStatus(brandSlug, shopId),
    enabled: brandSlug.length > 0 && shopId.length > 0,
    refetchInterval: 60_000,
  });

  if (isLoading) {
    return (
      <span
        style={{
          display: 'inline-block',
          padding: '0.25rem 0.75rem',
          borderRadius: '9999px',
          fontSize: '0.8125rem',
          fontWeight: 600,
          background: '#f3f4f6',
          color: '#6b7280',
        }}
      >
        …
      </span>
    );
  }

  if (isError || data === undefined) {
    return null;
  }

  if (data.isOpen) {
    return (
      <span
        style={{
          display: 'inline-block',
          padding: '0.25rem 0.75rem',
          borderRadius: '9999px',
          fontSize: '0.8125rem',
          fontWeight: 600,
          background: '#d1fae5',
          color: '#065f46',
        }}
      >
        Open
      </span>
    );
  }

  const nextOpenLabel =
    data.nextOpeningTime !== null
      ? `Opens at ${data.nextOpeningTime}`
      : 'Closed';

  return (
    <span
      style={{
        display: 'inline-block',
        padding: '0.25rem 0.75rem',
        borderRadius: '9999px',
        fontSize: '0.8125rem',
        fontWeight: 600,
        background: '#fee2e2',
        color: '#991b1b',
      }}
    >
      {nextOpenLabel}
    </span>
  );
}
