import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { usePlatformAdmins, useInvitePlatformAdmin, useDeactivatePlatformAdmin } from '../hooks/usePlatformAdmins';
import type { PlatformAdmin } from '../../../types/common';

// ---------------------------------------------------------------------------
// Validation helpers
// ---------------------------------------------------------------------------

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

interface InviteFormErrors {
  email?: string;
  displayName?: string;
}

// ---------------------------------------------------------------------------
// Sub-component: a single row that owns its own deactivate mutation
// ---------------------------------------------------------------------------

interface AdminRowProps {
  admin: PlatformAdmin;
  onDeactivate: (admin: PlatformAdmin) => void;
}

function AdminRow({ admin, onDeactivate }: AdminRowProps) {
  const { t } = useTranslation();

  return (
    <tr style={{ borderBottom: '1px solid #e5e7eb' }}>
      <td style={{ padding: '0.75rem 1rem' }}>{admin.displayName}</td>
      <td style={{ padding: '0.75rem 1rem', color: '#6b7280' }}>{admin.email}</td>
      <td style={{ padding: '0.75rem 1rem' }}>
        {admin.isPlatformAdmin && (
          <span
            style={{
              display: 'inline-block',
              padding: '0.125rem 0.5rem',
              borderRadius: '9999px',
              fontSize: '0.75rem',
              fontWeight: 600,
              background: '#dbeafe',
              color: '#1e40af',
            }}
          >
            Platform Admin
          </span>
        )}
      </td>
      <td style={{ padding: '0.75rem 1rem' }}>
        <button
          onClick={() => onDeactivate(admin)}
          style={{
            padding: '0.25rem 0.75rem',
            fontSize: '0.875rem',
            borderRadius: '0.25rem',
            border: '1px solid #d1d5db',
            background: '#fff',
            cursor: 'pointer',
          }}
        >
          {t('admin.platformAdmins.deactivate')}
        </button>
      </td>
    </tr>
  );
}

// ---------------------------------------------------------------------------
// Sub-component: inline invite form
// ---------------------------------------------------------------------------

interface InviteFormProps {
  onCancel: () => void;
}

function InviteForm({ onCancel }: InviteFormProps) {
  const { t } = useTranslation();
  const invite = useInvitePlatformAdmin();

  const [email, setEmail] = useState('');
  const [displayName, setDisplayName] = useState('');
  const [errors, setErrors] = useState<InviteFormErrors>({});
  const [apiError, setApiError] = useState<string | null>(null);

  function validate(): InviteFormErrors {
    const next: InviteFormErrors = {};
    if (email.trim().length === 0) {
      next.email = t('admin.platformAdmins.email') + ' is required.';
    } else if (!EMAIL_PATTERN.test(email)) {
      next.email = 'Enter a valid email address.';
    }
    if (displayName.trim().length === 0) {
      next.displayName = t('admin.platformAdmins.displayName') + ' is required.';
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
      { email: email.trim(), displayName: displayName.trim() },
      {
        onSuccess: () => {
          onCancel();
        },
        onError: (err) => {
          setApiError(err instanceof Error ? err.message : 'Unknown error.');
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
        maxWidth: '480px',
      }}
    >
      <h2 style={{ margin: '0 0 1rem', fontSize: '1.1rem', fontWeight: 600 }}>
        {t('admin.platformAdmins.invite')}
      </h2>

      <div style={{ marginBottom: '0.75rem' }}>
        <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.25rem', fontWeight: 500 }}>
          {t('admin.platformAdmins.email')}
        </label>
        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
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

      <div style={{ marginBottom: '1rem' }}>
        <label style={{ display: 'block', fontSize: '0.875rem', marginBottom: '0.25rem', fontWeight: 500 }}>
          {t('admin.platformAdmins.displayName')}
        </label>
        <input
          type="text"
          value={displayName}
          onChange={(e) => setDisplayName(e.target.value)}
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
          {invite.isPending ? '…' : t('admin.platformAdmins.invite')}
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
          {t('admin.platformAdmins.cancel')}
        </button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Sub-component: confirmation dialog
// ---------------------------------------------------------------------------

interface ConfirmDeactivateDialogProps {
  admin: PlatformAdmin;
  onConfirm: () => void;
  onCancel: () => void;
  isPending: boolean;
  apiError: string | null;
}

function ConfirmDeactivateDialog({ admin, onConfirm, onCancel, isPending, apiError }: ConfirmDeactivateDialogProps) {
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
          {t('admin.platformAdmins.confirmDeactivate', { name: admin.displayName })}
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
            {t('admin.platformAdmins.cancel')}
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
            {isPending ? '…' : t('admin.platformAdmins.deactivate')}
          </button>
        </div>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Main page
// ---------------------------------------------------------------------------

export function PlatformAdminList() {
  const { t } = useTranslation();
  const { data: admins, isLoading, isError, error } = usePlatformAdmins();
  const deactivate = useDeactivatePlatformAdmin();

  const [showInviteForm, setShowInviteForm] = useState(false);
  const [pendingDeactivate, setPendingDeactivate] = useState<PlatformAdmin | null>(null);
  const [deactivateError, setDeactivateError] = useState<string | null>(null);

  function handleDeactivateClick(admin: PlatformAdmin) {
    setPendingDeactivate(admin);
    setDeactivateError(null);
  }

  function handleDeactivateConfirm() {
    if (!pendingDeactivate) return;

    deactivate.mutate(pendingDeactivate.id, {
      onSuccess: () => {
        setPendingDeactivate(null);
        setDeactivateError(null);
      },
      onError: (err) => {
        setDeactivateError(err instanceof Error ? err.message : t('admin.platformAdmins.lastAdminError'));
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
          {t('admin.platformAdmins.title')}
        </h1>
        {!showInviteForm && (
          <button
            onClick={() => setShowInviteForm(true)}
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
            + {t('admin.platformAdmins.invite')}
          </button>
        )}
      </div>

      {showInviteForm && (
        <InviteForm onCancel={() => setShowInviteForm(false)} />
      )}

      {isLoading && <p style={{ color: '#6b7280' }}>{t('loading')}</p>}

      {isError && (
        <p style={{ color: '#dc2626' }}>
          {t('error')}:{' '}
          {error instanceof Error ? error.message : 'Unknown error'}
        </p>
      )}

      {!isLoading && !isError && admins !== undefined && admins.length === 0 && (
        <p style={{ color: '#6b7280' }}>{t('admin.platformAdmins.empty')}</p>
      )}

      {!isLoading && !isError && admins !== undefined && admins.length > 0 && (
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
                  {t('admin.platformAdmins.displayName')}
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>
                  {t('admin.platformAdmins.email')}
                </th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Status</th>
                <th style={{ padding: '0.5rem 1rem', fontWeight: 600 }}>Actions</th>
              </tr>
            </thead>
            <tbody>
              {admins.map((admin) => (
                <AdminRow key={admin.id} admin={admin} onDeactivate={handleDeactivateClick} />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {pendingDeactivate && (
        <ConfirmDeactivateDialog
          admin={pendingDeactivate}
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
