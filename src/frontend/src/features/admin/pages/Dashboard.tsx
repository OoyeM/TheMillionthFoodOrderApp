import { useNavigate, useParams } from 'react-router-dom';

export function AdminDashboard() {
  const navigate = useNavigate();
  const { brandSlug, lang } = useParams<{ brandSlug: string; lang: string }>();

  return (
    <main style={{ padding: '1.5rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '0.5rem' }}>
        Admin Dashboard
      </h1>
      <p style={{ color: '#6b7280', marginBottom: '2rem' }}>
        CMS administration panel — manage brands, shops, products and orders.
      </p>

      <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
        <NavCard
          title="Brands"
          description="Manage brands on the platform."
          onClick={() => { navigate(`/${brandSlug}/${lang}/admin/brands`); }}
        />
        <NavCard
          title="Manage Shops"
          description="Create and manage shops within this brand."
          onClick={() => { navigate(`/${brandSlug}/${lang}/admin/shops`); }}
        />
      </div>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Sub-component: navigation card
// ---------------------------------------------------------------------------

interface NavCardProps {
  title: string;
  description: string;
  onClick: () => void;
}

function NavCard({ title, description, onClick }: NavCardProps) {
  return (
    <button
      onClick={onClick}
      style={{
        padding: '1.25rem 1.5rem',
        background: '#fff',
        border: '1px solid #e5e7eb',
        borderRadius: '0.5rem',
        cursor: 'pointer',
        textAlign: 'left',
        minWidth: '14rem',
        boxShadow: '0 1px 2px rgba(0,0,0,0.05)',
      }}
    >
      <p style={{ fontWeight: 700, fontSize: '1rem', margin: '0 0 0.25rem' }}>{title}</p>
      <p style={{ fontSize: '0.875rem', color: '#6b7280', margin: 0 }}>{description}</p>
    </button>
  );
}
