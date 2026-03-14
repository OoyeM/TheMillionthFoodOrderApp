import { Outlet } from 'react-router-dom';
import type { AppVariant } from './useAppVariant';

interface Props {
  variant: AppVariant;
}

const VARIANT_LABELS: Record<AppVariant, string> = {
  storefront: 'Storefront',
  pos: 'Point of Sale',
  admin: 'Admin',
};

/**
 * Minimal layout wrapper that indicates the active app variant.
 * In later iterations this will be replaced by variant-specific navigation.
 */
export function AppVariantLayout({ variant }: Props) {
  return (
    <div>
      <header
        style={{
          padding: '0.75rem 1rem',
          borderBottom: '1px solid #e5e7eb',
          display: 'flex',
          alignItems: 'center',
          gap: '0.5rem',
        }}
      >
        <span style={{ fontWeight: 600 }}>{VARIANT_LABELS[variant]}</span>
      </header>
      <Outlet />
    </div>
  );
}
