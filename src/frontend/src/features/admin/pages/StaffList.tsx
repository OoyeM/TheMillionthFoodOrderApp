import { useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useBrandStaff, useInviteBrandStaff, useDeactivateBrandStaff } from '../hooks/useBrandStaff';
import { useShops } from '../hooks/useShops';
import type { StaffMember, Shop } from '../../../types/common';
import { StaffRoleValue, ShopLevelRoles } from '../../../types/common';
import type { StaffRole } from '../../../types/common';

// ---------------------------------------------------------------------------
// Role helpers
// ---------------------------------------------------------------------------

/** Roles available in the staff management UI (Customer excluded — self-registration). */
const STAFF_ROLES: StaffRole[] = [
  'BrandAdmin',
  'ShopManager',
  'CounterStaff',
  'KitchenStaff',
  'FloorStaff',
];

/** All roles including Customer — for display/lookup only. */
const ALL_ROLES: StaffRole[] = [...STAFF_ROLES, 'Customer'];

/** Maps numeric role values (from API) back to the role name string. */
const ROLE_BY_VALUE = new Map<number, StaffRole>(
  ALL_ROLES.map((r) => [StaffRoleValue[r], r]),
);

function roleNameFromValue(value: number): StaffRole {
  return ROLE_BY_VALUE.get(value) ?? 'Customer';
}

const ROLE_BADGE_STYLES: Record<StaffRole, { background: string; color: string }> = {
  BrandAdmin: { background: '#dbeafe', color: '#1e40af' },
  ShopManager: { background: '#ede9fe', color: '#5b21b6' },
  CounterStaff: { background: '#d1fae5', color: '#065f46' },
  KitchenStaff: { background: '#fef3c7', color: '#92400e' },
  FloorStaff: { background: '#fce7f3', color: '#9d174d' },
  Customer: { background: '#f3f4f6', color: '#374151' },
};

// ---------------------------------------------------------------------------
// Validation helpers
// ---------------------------------------------------------------------------

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface InviteFormErrors {
  email?: string;
  displayName?: string;
  role?: string;
  shopId?: string;
}

// ---------------------------------------------------------------------------
// Sub-component: Role badge
// ---------------------------------------------------------------------------

interface RoleBadgeProps {
  role: StaffRole;
}

function RoleBadge({ role }: RoleBadgeProps) {
  const { t } = useTranslation();
  const style = ROLE_BADGE_STYLES[role];

  return (
    <span
      style={{
        display: 'inline-block',
        padding: '0.125rem 0.5rem',
        borderRadius: '9999px',
        fontSize: '0.75rem',
        fontWeight: 600,
        background: style.background,
        color: style.color,
      }}
    >
      {t(`admin.staff.roles.${role}`)}
    </span>
  );
}

// ---------------------------------------------------------------------------
// Sub-component: a single staff row
// ---------------------------------------------------------------------------

interface StaffRowProps {
  member: StaffMember;
  onDeactivate: (member: StaffMember) => void;
}

function StaffRow({ member, onDeactivate }: StaffRowProps) {
  const { t } = useTranslation();
  const role = roleNameFromValue(member.role);

  return (
    <tr style={{ borderBottom: '1px solid #e5e7eb' }}>
      <td style={{ padding: '0.75rem 1rem' }}>{member.displayName}</td>
      <td style={{ padding: '0.75rem 1rem', color: '#6b7280' }}>{member.email}</td>
      <td style={{ padding: '0.75rem 1rem' }}>
        <RoleBadge role={role} />
      </td>
      <td style={{ padding: '0.75rem 1rem', color: '#6b7280' }}>
        {member.shopName ?? '—'}
      </td>
      <td style={{ padding: '0.75rem 1rem' }}>
        <button
          onClick={() => { onDeactivate(member); }}
          style={{
            padding: '0.25rem 0.75rem',
            fontSize: '0.875rem',
            borderRadius: '0.25rem',
            border: '1px solid #d1d5db',
            background: '#fff',
            cursor: 'pointer',
          }}
        >
          {t('admin.staff.deactivate')}
        </button>
      </td>
    </tr>
  );
}

// ---------------------------------------------------------------------------
// Sub-component: inline invite form
// ---------------------------------------------------------------------------

interface InviteFormProps {
  brandSlug: string;
  shops: Shop[];
  onCancel: () => void;
}

function InviteForm({ brandSlug, shops, onCancel }: InviteFormProps) {
  const { t } = useTranslation();
  const invite = useInviteBrandStaff(brandSlug);

  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [role, setRole] = useState<StaffRole>('BrandAdmin');
  const [shopId, setShopId] = useState<string>('');
  const [errors, setErrors] = useState<InviteFormErrors>({});
  const [apiError, setApiError] = useState<string | null>(null);

  const isShopLevelRole = ShopLevelRoles.has(role);

  function validate(): InviteFormErrors {
    const next: InviteFormErrors = {};

    if (email.trim().length === 0) {
      next.email = t('admin.staff.form.emailRequired');
    } else if (!EMAIL_PATTERN.test(email)) {
      next.email = t('admin.staff.form.emailInvalid');
    }

    if (displayName.trim().length === 0) {
      next.displayName = t('admin.staff.form.displayNameRequired');
    }

    if (isShopLevelRole && shopId.trim().length === 0) {
      next.shopId = t('admin.staff.form.shopRequired');
    }

    return next;
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const validationErrors = validate();
    if (Object.keys(validationErrors).length > 0) {
      setErrors(validationErrors);
      return;
    }

    setErrors({});
    setApiError(null);

    invite.mutate(
      {
        email: email.trim(),
        displayName: displayName.trim(),
        role: StaffRoleValue[role],
        shopId: isShopLevelRole ? shopId : null,
      },
      {
        onSuccess: () => {
          onCancel();
        },
        onError: (err) => {
          setApiError(err instanceof Error ? err.message : t('admin.staff.form.unknownError'));
        },
      },
    );
  }

  return (
    <form
      onSubmit={handleSubmit}
      style={{
        background: '#f9fafb',
        border: '1px solid #e5e7eb',
        borderRadius: '0.5rem',
        padding: '1.25rem',
        marginBottom: '1.5rem',
        maxWidth: '520px',
      }}
    >
      <h2 style={{ margin: '0 0 1rem', fontSize: '1.1rem', fontWeight: 600 }}>
        {t('admin.staff.invite')}
      </h2>

      {/* Email */}
      <div style={{ marginBottom: '0.75rem' }}>
        <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.25rem', fontWeight: 500 }}>
          {t('admin.staff.form.email')}
        </label>
        <input
          type="email"
          value={email}
          onChange={(e) => { setEmail(e.target.value); }}
          style={{
            width: '100%',
            padding: '0.5rem 0.75rem',
            border: errors.email ? '1px solid #dc2626' : '1px solid #d1d5db',
            borderRadius: '0.375rem',
            fontSize: '0.875rem',
            boxSizing: 'border-box',
          }}
        />
        {errors.email && (
          <p style={{ margin: '0.25rem 0 0', color: '#dc2626', fontSize: '0.75rem' }}>{errors.email}</p>
        )}
      </div>

      {/* Display name */}
      <div style={{ marginBottom: '0.75rem' }}>
        <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.25rem', fontWeight: 500 }}>
          {t('admin.staff.form.displayName')}
        </label>
        <input
          type="text"
          value={displayName}
          onChange={(e) => { setDisplayName(e.target.value); }}
          style={{
            width: '100%',
            padding: '0.5rem 0.75rem',
            border: errors.displayName ? '1px solid #dc2626' : '1px solid #d1d5db',
            borderRadius: '0.375rem',
            fontSize: '0.875rem',
            boxSizing: 'border-box',
          }}
        />
        {errors.displayName && (
          <p style={{ margin: '0.25rem 0 0', color: '#dc2626', fontSize: '0.75rem' }}>{errors.displayName}</p>
        )}
      </div>

      {/* Role dropdown */}
      <div style={{ marginBottom: '0.75rem' }}>
        <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.25rem', fontWeight: 500 }}>
          {t('admin.staff.form.role')}
        </label>
        <select
          value={role}
          onChange={(e) => {
            setRole(e.target.value as StaffRole);
            setShopId('');
          }}
          style={{
            width: '100%',
            padding: '0.5rem 0.75rem',
            border: '1px solid #d1d5db',
            borderRadius: '0.375rem',
            fontSize: '0.875rem',
            boxSizing: 'border-box',
            background: '#fff',
          }}
        >
          {STAFF_ROLES.map((r) => (
            <option key={r} value={r}>
              {t(`admin.staff.roles.${r}`)}
            </option>
          ))}
        </select>
        {errors.role && (
          <p style={{ margin: '0.25rem 0 0', color: '#dc2626', fontSize: '0.75rem' }}>{errors.role}</p>
        )}
      </div>

      {/* Shop dropdown — only shown for shop-level roles */}
      {isShopLevelRole && (
        <div style={{ marginBottom: '0.75rem' }}>
          <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.25rem', fontWeight: 500 }}>
            {t('admin.staff.form.shop')}
          </label>
          <select
            value={shopId}
            onChange={(e) => { setShopId(e.target.value); }}
            style={{
              width: '100%',
              padding: '0.5rem 0.75rem',
              border: errors.shopId ? '1px solid #dc2626' : '1px solid #d1d5db',
              borderRadius: '0.375rem',
              fontSize: '0.875rem',
              boxSizing: 'border-box',
              background: '#fff',
            }}
          >
            <option value="">{t('admin.staff.form.selectShop')}</option>
            {shops.map((s) => (
              <option key={s.id} value={s.id}>
                {s.name}
              </option>
            ))}
          </select>
          {errors.shopId && (
            <p style={{ margin: '0.25rem 0 0', color: '#dc2626', fontSize: '0.75rem' }}>{errors.shopId}</p>
          )}
        </div>
      )}

      {apiError && (
        <p style={{ margin: '0 0 0.75rem', color: '#dc2626', fontSize: '0.875rem' }}>{apiError}</p>
      )}

      <div style={{ display: 'flex', gap: '0.5rem' }}>
        <button
          type="submit"
          disabled={invite.isPending}
          style={{
            padding: '0.5rem 1rem',
            background: '#111827',
            color: '#fff',
            border: 'none',
            borderRadius: '0.375rem',
            cursor: invite.isPending ? 'not-allowed' : 'pointer',
            opacity: invite.isPending ? 0.6 : 1,
            fontWeight: 600,
            fontSize: '0.875rem',
          }}
        >
          {invite.isPending ? '…' : t('admin.staff.invite')}
        </button>
        <button
          type="button"
          onClick={onCancel}
          style={{
            padding: '0.5rem 1rem',
            background: '#fff',
            border: '1px solid #d1d5db',
            borderRadius: '0.375rem',
            cursor: 'pointer',
            fontSize: '0.875rem',
          }}
        >
          {t('admin.staff.cancel')}
        </button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Sub-component: confirmation dialog
// ---------------------------------------------------------------------------

interface ConfirmDeactivateDialogProps {
  member: StaffMember;
  onConfirm: () => void;
  onCancel: () => void;
  isPending: boolean;
  apiError: string | null;
}

function ConfirmDeactivateDialog({
  member,
  onConfirm,
  onCancel,
  isPending,
  apiError,
}: ConfirmDeactivateDialogProps) {
  const { t } = useTranslation();

  return (
    <div
      style={{
        position: 'fixed',
        inset: 0,
        background: 'rgba(0,0,0,0.4)',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        zIndex: 1000,
      }}
    >
      <div
        style={{
          background: '#fff',
          borderRadius: '0.5rem',
          padding: '1.5rem',
          maxWidth: '420px',
          width: '100%',
          boxShadow: '0 10px 25px rgba(0,0,0,0.15)',
        }}
      >
        <p style={{ margin: '0 0 1rem', fontSize: '0.95rem' }}>
          {t('admin.staff.confirmDeactivate', { name: member.displayName })}
        </p>

        {apiError && (
          <p style={{ margin: '0 0 0.75rem', color: '#dc2626', fontSize: '0.875rem' }}>{apiError}</p>
        )}

        <div style={{ display: 'flex', gap: '0.5rem', justifyContent: 'flex-end' }}>
          <button
            onClick={onCancel}
            style={{
              padding: '0.5rem 1rem',
              background: '#fff',
              border: '1px solid #d1d5db',
              borderRadius: '0.375rem',
              cursor: 'pointer',
              fontSize: '0.875rem',
            }}
          >
            {t('admin.staff.cancel')}
          </button>
          <button
            onClick={onConfirm}
            disabled={isPending}
            style={{
              padding: '0.5rem 1rem',
              background: '#dc2626',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: isPending ? 'not-allowed' : 'pointer',
              opacity: isPending ? 0.6 : 1,
              fontWeight: 600,
              fontSize: '0.875rem',
            }}
          >
            {isPending ? '…' : t('admin.staff.deactivate')}
          </button>
        </div>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------

export function StaffList() {
  const { t } = useTranslation();
  const { brandSlug } = useParams<{ brandSlug: string }>();
  const resolvedBrandSlug = brandSlug ?? '';

  const { data: staff, isLoading, isError, error } = useBrandStaff(resolvedBrandSlug);
  const { data: shops = [] } = useShops(resolvedBrandSlug);
  const deactivate = useDeactivateBrandStaff(resolvedBrandSlug);

  const [showInviteForm, setShowInviteForm] = useState(false);
  const [pendingDeactivate, setPendingDeactivate] = useState<StaffMember | null>(null);
  const [deactivateError, setDeactivateError] = useState<string | null>(null);

  function handleDeactivateClick(member: StaffMember) {
    setPendingDeactivate(member);
    setDeactivateError(null);
  }

  function handleDeactivateConfirm() {
    if (!pendingDeactivate) return;

    deactivate.mutate(pendingDeactivate.roleId, {
      onSuccess: () => {
        setPendingDeactivate(null);
        setDeactivateError(null);
      },
      onError: (err) => {
        setDeactivateError(
          err instanceof Error ? err.message : t('admin.staff.lastAdminError'),
        );
      },
    });
  }

  return (
    <main style={{ padding: '1.5rem' }}>
      <div
        style={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          marginBottom: '1.5rem',
        }}
      >
        <h1 style={{ margin: 0, fontSize: '1.5rem', fontWeight: 700 }}>
          {t('admin.staff.title')}
        </h1>
        {!showInviteForm && (
          <button
            onClick={() => { setShowInviteForm(true); }}
            style={{
              padding: '0.5rem 1rem',
              background: '#111827',
              color: '#fff',
              border: 'none',
              borderRadius: '0.375rem',
              cursor: 'pointer',
              fontWeight: 600,
            }}
          >
            + {t('admin.staff.invite')}
          </button>
        )}
      </div>

      {showInviteForm && (
        <InviteForm
          brandSlug={resolvedBrandSlug}
          shops={shops}
          onCancel={() => { setShowInviteForm(false); }}
        />
      )}

      {isLoading && <p style={{ color: '#6b7280' }}>{t('loading')}</p>}

      {isError && (
        <p style={{ color: '#dc2626' }}>
          {t('error')}:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
      )}

      {!isLoading && !isError && staff?.length === 0 && (
        <p style={{ color: '#6b7280' }}>{t('admin.staff.empty')}</p>
      )}

      {!isLoading && !isError && staff !== undefined && staff.length > 0 && (
        <div style={{ overflowX: 'auto' }}>
          <table
            style={{
              width: '100%',
              borderCollapse: 'collapse',
              fontSize: '0.9rem',
            }}
          >
            <thead>
              <tr style={{ borderBottom: '2px solid #e5e7eb', textAlign: 'left' }}>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>
                  {t('admin.staff.columns.displayName')}
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>
                  {t('admin.staff.columns.email')}
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>
                  {t('admin.staff.columns.role')}
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>
                  {t('admin.staff.columns.shop')}
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>
                  {t('admin.staff.columns.actions')}
                </th>
              </tr>
            </thead>
            <tbody>
              {staff.map((member) => (
                <StaffRow
                  key={member.roleId}
                  member={member}
                  onDeactivate={handleDeactivateClick}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {pendingDeactivate && (
        <ConfirmDeactivateDialog
          member={pendingDeactivate}
          onConfirm={handleDeactivateConfirm}
          onCancel={() => {
            setPendingDeactivate(null);
            setDeactivateError(null);
          }}
          isPending={deactivate.isPending}
          apiError={deactivateError}
        />
      )}
    </main>
  );
}
