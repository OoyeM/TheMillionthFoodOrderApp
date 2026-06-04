import type React from 'react';
import { secondaryButtonStyle } from './adminFormStyles';

// ---------------------------------------------------------------------------
// ResourceFormShell
//
// Wraps the loading / error / content pattern shared by every Edit page.
// ---------------------------------------------------------------------------

interface ResourceFormShellProps {
  isFetching: boolean;
  fetchError: unknown;
  resourceName: string;
  onCancel: () => void;
  children: React.ReactNode;
}

export function ResourceFormShell({
  isFetching,
  fetchError,
  resourceName,
  onCancel,
  children,
}: ResourceFormShellProps): React.JSX.Element {
  if (isFetching) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>Loading {resourceName}…</p>
      </main>
    );
  }

  if (fetchError !== null) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>
          Failed to load {resourceName}:{' '}
          {fetchError instanceof Error ? fetchError.message : 'Unknown error'}
        </p>
        <button onClick={onCancel} style={secondaryButtonStyle}>
          Back to list
        </button>
      </main>
    );
  }

  return <>{children}</>;
}
