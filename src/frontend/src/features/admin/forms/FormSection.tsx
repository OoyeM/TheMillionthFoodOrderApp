import { type ReactNode, useState } from 'react';

interface FormSectionProps {
  title: string;
  description?: string;
  defaultOpen?: boolean;
  children: ReactNode;
}

/**
 * A collapsible labelled card grouping related form fields.
 * Used by the admin Edit/Create pages to break long forms into chunks.
 */
export function FormSection({
  title,
  description,
  defaultOpen = true,
  children,
}: FormSectionProps): JSX.Element {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  return (
    <section
      className="form-section"
      style={{
        border: '1px solid #e5e7eb',
        borderRadius: 8,
        padding: 16,
        marginBottom: 16,
      }}
    >
      <header
        style={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          cursor: 'pointer',
        }}
        onClick={() => { setIsOpen((v) => !v); }}
        role="button"
        aria-expanded={isOpen}
        tabIndex={0}
        onKeyDown={(e) => {
          if (e.key === 'Enter' || e.key === ' ') {
            e.preventDefault();
            setIsOpen((v) => !v);
          }
        }}
      >
        <div>
          <h3 style={{ margin: 0 }}>{title}</h3>
          {description !== undefined && (
            <p style={{ margin: '4px 0 0 0', color: '#6b7280', fontSize: 14 }}>{description}</p>
          )}
        </div>
        <span aria-hidden="true">{isOpen ? '▼' : '▶'}</span>
      </header>
      {isOpen && <div style={{ marginTop: 12 }}>{children}</div>}
    </section>
  );
}
