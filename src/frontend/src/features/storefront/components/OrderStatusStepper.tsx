// Presentational stepper component — renders the shop's configured order
// lifecycle as a horizontal step progression for customer-facing tracking.

import type { OrderStatusResponse } from '@api/orders';

interface Props {
  /** Statuses from the lifecycle, ordered by sortOrder. */
  statuses: OrderStatusResponse[];
  /** The current status name of the order. */
  currentStatusName: string;
}

/**
 * Renders the order lifecycle as a visual step indicator.
 * Completed steps are filled; the current step is highlighted with the brand
 * colour (or the status colorHex); upcoming steps are greyed out.
 */
export function OrderStatusStepper({ statuses, currentStatusName }: Props) {
  const enabled = statuses.filter((s) => s.isEnabled).sort((a, b) => a.sortOrder - b.sortOrder);

  const currentIndex = enabled.findIndex(
    (s) => s.name.toLowerCase() === currentStatusName.toLowerCase(),
  );

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'flex-start',
        overflowX: 'auto',
        padding: '0.5rem 0 1rem',
        gap: 0,
      }}
      role="list"
      aria-label="Order status progression"
    >
      {enabled.map((status, idx) => {
        const isCompleted = idx < currentIndex;
        const isCurrent = idx === currentIndex;
        const isPending = idx > currentIndex;

        const brandColor = status.colorHex ?? 'var(--brand-color-primary, #111827)';
        const stepColor = isCurrent || isCompleted ? brandColor : '#d1d5db';
        const textColor = isCurrent ? brandColor : isCompleted ? '#374151' : '#9ca3af';
        const fontWeight = isCurrent ? 700 : 400;

        return (
          <div
            key={status.id}
            role="listitem"
            style={{
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              flex: '1 0 5rem',
              minWidth: '4rem',
              position: 'relative',
            }}
          >
            {/* Connector line before this step */}
            {idx > 0 && (
              <div
                aria-hidden="true"
                style={{
                  position: 'absolute',
                  top: '0.875rem',
                  right: '50%',
                  left: '-50%',
                  height: '2px',
                  background: isCompleted || isCurrent ? brandColor : '#e5e7eb',
                  zIndex: 0,
                }}
              />
            )}

            {/* Step circle */}
            <div
              aria-hidden="true"
              style={{
                position: 'relative',
                zIndex: 1,
                width: '1.75rem',
                height: '1.75rem',
                borderRadius: '50%',
                border: `2px solid ${stepColor}`,
                background: isCompleted || isCurrent ? stepColor : '#fff',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                flexShrink: 0,
                transition: 'background 0.2s, border-color 0.2s',
              }}
            >
              {isCompleted && (
                <svg
                  width="12"
                  height="12"
                  viewBox="0 0 12 12"
                  fill="none"
                  aria-hidden="true"
                >
                  <path
                    d="M2 6l3 3 5-5"
                    stroke="#fff"
                    strokeWidth="1.8"
                    strokeLinecap="round"
                    strokeLinejoin="round"
                  />
                </svg>
              )}
              {isCurrent && (
                <div
                  style={{
                    width: '0.5rem',
                    height: '0.5rem',
                    borderRadius: '50%',
                    background: '#fff',
                  }}
                />
              )}
              {isPending && (
                <div
                  style={{
                    width: '0.375rem',
                    height: '0.375rem',
                    borderRadius: '50%',
                    background: '#d1d5db',
                  }}
                />
              )}
            </div>

            {/* Status label */}
            <span
              style={{
                marginTop: '0.375rem',
                fontSize: '0.6875rem',
                fontWeight,
                color: textColor,
                textAlign: 'center',
                lineHeight: 1.3,
                wordBreak: 'break-word',
                maxWidth: '5rem',
                transition: 'color 0.2s',
              }}
            >
              {status.name}
            </span>
          </div>
        );
      })}
    </div>
  );
}
