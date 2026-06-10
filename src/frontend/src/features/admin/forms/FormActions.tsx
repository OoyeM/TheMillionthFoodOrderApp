import type React from 'react';
import { primaryButtonStyle, secondaryButtonStyle } from './adminFormStyles';

// ---------------------------------------------------------------------------
// FormActions — the submit / cancel button pair shared by every admin
// create/edit form. `isPending` (the mutation state) drives the submit label;
// `isSubmitting` (react-hook-form) additionally disables the button when a
// page tracks both.
// ---------------------------------------------------------------------------

interface FormActionsProps {
  isPending: boolean;
  isSubmitting?: boolean;
  onCancel: () => void;
  submitLabel: string;
  pendingLabel: string;
  cancelLabel?: string;
}

export function FormActions({
  isPending,
  isSubmitting = false,
  onCancel,
  submitLabel,
  pendingLabel,
  cancelLabel = 'Cancel',
}: FormActionsProps): React.JSX.Element {
  const busy = isSubmitting || isPending;
  return (
    <div style={{ display: 'flex', gap: '0.75rem' }}>
      <button type="submit" disabled={busy} style={primaryButtonStyle(busy)}>
        {isPending ? pendingLabel : submitLabel}
      </button>
      <button type="button" onClick={onCancel} style={secondaryButtonStyle}>
        {cancelLabel}
      </button>
    </div>
  );
}
