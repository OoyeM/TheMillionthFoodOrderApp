import { Outlet, NavLink, useParams } from 'react-router-dom';
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
 * For the admin variant, renders a top navigation bar with links to all admin sections.
 */
export function AppVariantLayout({ variant }: Props) {
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();

  return (
    <div>
      <header
        style={{
          padding: '0.75rem 1rem',
          borderBottom: '1px solid #e5e7eb',
          display: 'flex',
          alignItems: 'center',
          gap: '1rem',
        }}
      >
        <span style={{ fontWeight: 600 }}>{VARIANT_LABELS[variant]}</span>

        {variant === 'admin' && brandSlug && lang && (
          <nav style={{ display: 'flex', gap: '0.25rem', alignItems: 'center' }}>
            <AdminNavLink to={`/${brandSlug}/${lang}/admin/brands`} label="Brands" />
            <AdminNavLink to={`/${brandSlug}/${lang}/admin/shops`} label="Shops" />
            <AdminNavLink to={`/${brandSlug}/${lang}/admin/products`} label="Products" />
            <AdminNavLink
              to={`/${brandSlug}/${lang}/admin/modifier-groups`}
              label="Modifier Groups"
            />
            <AdminNavLink
              to={`/${brandSlug}/${lang}/admin/menu-categories`}
              label="Menu Categories"
            />
            <AdminNavLink
              to={`/${brandSlug}/${lang}/admin/platform-admins`}
              label="Admins"
            />
          </nav>
        )}
      </header>
      <Outlet />
    </div>
  );
}

// ---------------------------------------------------------------------------
// Admin nav link sub-component
// ---------------------------------------------------------------------------

interface AdminNavLinkProps {
  to: string;
  label: string;
}

function AdminNavLink({ to, label }: AdminNavLinkProps) {
  return (
    <NavLink
      to={to}
      style={({ isActive }) => ({
        padding: '0.25rem 0.75rem',
        fontSize: '0.875rem',
        borderRadius: '0.375rem',
        textDecoration: 'none',
        fontWeight: isActive ? 600 : 400,
        background: isActive ? '#111827' : 'transparent',
        color: isActive ? '#fff' : '#374151',
      })}
    >
      {label}
    </NavLink>
  );
}
