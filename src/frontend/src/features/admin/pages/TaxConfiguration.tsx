import { useState, useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useTaxConfiguration, useUpdateTaxConfiguration } from '../hooks/useTaxConfiguration';
import { CONSUMPTION_MODES } from '../../../types/common';
import type { ConsumptionMode } from '../../../types/common';

// Default Belgian VAT rates
const BELGIAN_VAT_DEFAULTS: Record<ConsumptionMode, number> = {
  Takeaway: 6,
  EatIn: 21,
};

// ---------------------------------------------------------------------------
// Page component
// ---------------------------------------------------------------------------

export function TaxConfiguration() {
  const { brandSlug } = useParams<{ brandSlug: string; lang: string }>();
  const resolvedSlug = brandSlug ?? '';
  const { t } = useTranslation('common');

  const { data: taxConfig, isLoading, isError } = useTaxConfiguration(resolvedSlug);
  const updateMutation = useUpdateTaxConfiguration(resolvedSlug);

  // ── Form state ────────────────────────────────────────────────────────────
  const [rates, setRates] = useState<Record<ConsumptionMode, number>>({
    Takeaway: BELGIAN_VAT_DEFAULTS.Takeaway,
    EatIn: BELGIAN_VAT_DEFAULTS.EatIn,
  });
  const [formInitialized, setFormInitialized] = useState(false);
  const [successMessage, setSuccessMessage] = useState('');

  // Example calculation state
  const [calcGrossAmount, setCalcGrossAmount] = useState('10.00');
  const [calcMode, setCalcMode] = useState<ConsumptionMode>('Takeaway');

  // Populate form when data arrives
  useEffect(() => {
    if (taxConfig !== undefined && !formInitialized) {
      const updated = { ...BELGIAN_VAT_DEFAULTS };
      for (const vatRate of taxConfig.vatRates) {
        updated[vatRate.consumptionMode] = vatRate.ratePercentage;
      }
      setRates(updated);
      setFormInitialized(true);
    }
  }, [taxConfig, formInitialized]);

  // Auto-dismiss success message
  useEffect(() => {
    if (!successMessage) return;
    const timer = setTimeout(() => setSuccessMessage(''), 3000);
    return () => clearTimeout(timer);
  }, [successMessage]);

  // ── Handlers ──────────────────────────────────────────────────────────────

  function handleRateChange(mode: ConsumptionMode, value: string) {
    const parsed = parseFloat(value);
    if (!isNaN(parsed)) {
      setRates((prev) => ({ ...prev, [mode]: parsed }));
    } else if (value === '' || value === '.') {
      setRates((prev) => ({ ...prev, [mode]: 0 }));
    }
  }

  function handleSave(e: React.FormEvent) {
    e.preventDefault();
    updateMutation.mutate(
      {
        vatRates: CONSUMPTION_MODES.map((mode) => ({
          consumptionMode: mode,
          ratePercentage: rates[mode],
        })),
      },
      {
        onSuccess: () => {
          setSuccessMessage(t('admin.taxConfiguration.saved'));
        },
      },
    );
  }

  // ── Client-side VAT breakdown calculation ────────────────────────────────

  function computeBreakdown(grossAmount: number, mode: ConsumptionMode) {
    const rate = rates[mode] / 100;
    // gross = net * (1 + rate)  →  net = gross / (1 + rate)
    const netAmount = grossAmount / (1 + rate);
    const vatAmount = grossAmount - netAmount;
    return { netAmount, vatAmount, grossAmount };
  }

  const parsedGross = parseFloat(calcGrossAmount);
  const hasValidGross = !isNaN(parsedGross) && parsedGross > 0;
  const breakdown = hasValidGross ? computeBreakdown(parsedGross, calcMode) : null;

  // ── Loading / error states ────────────────────────────────────────────────

  if (isLoading) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#6b7280' }}>{t('admin.taxConfiguration.loading')}</p>
      </main>
    );
  }

  if (isError) {
    return (
      <main style={{ padding: '1.5rem' }}>
        <p style={{ color: '#dc2626' }}>{t('admin.taxConfiguration.notConfigured')}</p>
      </main>
    );
  }

  // ── Form ──────────────────────────────────────────────────────────────────

  return (
    <main style={{ padding: '1.5rem', maxWidth: '48rem' }}>
      <h1 style={{ fontSize: '1.5rem', fontWeight: 700, marginBottom: '0.25rem' }}>
        {t('admin.taxConfiguration.title')}
      </h1>
      <p style={{ color: '#6b7280', fontSize: '0.875rem', marginBottom: '2rem' }}>
        {t('admin.taxConfiguration.description')}
      </p>

      {/* Success / error messages */}
      {successMessage && (
        <p
          style={{
            color: '#16a34a',
            marginBottom: '1rem',
            fontSize: '0.875rem',
            fontWeight: 500,
          }}
        >
          {successMessage}
        </p>
      )}

      {updateMutation.isError && (
        <p style={{ color: '#dc2626', marginBottom: '1rem', fontSize: '0.875rem' }}>
          {t('admin.taxConfiguration.saveError')}
        </p>
      )}

      <form onSubmit={handleSave} noValidate>
        {/* ── VAT rates per consumption mode ───────────────────────────── */}
        <section style={sectionStyle}>
          {CONSUMPTION_MODES.map((mode) => (
            <div
              key={mode}
              style={{
                display: 'flex',
                alignItems: 'center',
                gap: '1rem',
                marginBottom: '1rem',
                flexWrap: 'wrap',
              }}
            >
              <label htmlFor={`rate-${mode}`} style={{ flex: '1 1 16rem' }}>
                <span style={labelStyle}>
                  {t(`admin.taxConfiguration.consumptionModes.${mode}`)}
                </span>
              </label>
              <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                <input
                  id={`rate-${mode}`}
                  type="number"
                  step="0.01"
                  min="0"
                  max="100"
                  value={rates[mode]}
                  onChange={(e) => handleRateChange(mode, e.target.value)}
                  style={{ ...inputStyle, width: '7rem', textAlign: 'right' }}
                />
                <span style={{ fontSize: '0.875rem', color: '#6b7280' }}>%</span>
              </div>
            </div>
          ))}
        </section>

        {/* ── Save button ───────────────────────────────────────────────── */}
        <button
          type="submit"
          disabled={updateMutation.isPending}
          style={{
            padding: '0.5rem 1.5rem',
            background: '#111827',
            color: '#fff',
            border: 'none',
            borderRadius: '0.375rem',
            cursor: updateMutation.isPending ? 'not-allowed' : 'pointer',
            fontWeight: 600,
            opacity: updateMutation.isPending ? 0.6 : 1,
            marginBottom: '2rem',
          }}
        >
          {updateMutation.isPending
            ? t('admin.taxConfiguration.saving')
            : t('admin.taxConfiguration.save')}
        </button>
      </form>

      {/* ── Example calculation ───────────────────────────────────────── */}
      <section style={sectionStyle}>
        <h2 style={sectionHeadingStyle}>{t('admin.taxConfiguration.exampleCalculation')}</h2>

        <div
          style={{
            display: 'flex',
            gap: '1rem',
            alignItems: 'flex-end',
            flexWrap: 'wrap',
            marginBottom: '1rem',
          }}
        >
          {/* Gross amount input */}
          <div>
            <label htmlFor="calc-gross" style={labelStyle}>
              {t('admin.taxConfiguration.grossAmount')}
            </label>
            <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem', marginTop: '0.25rem' }}>
              <span style={{ fontSize: '0.875rem', color: '#6b7280' }}>€</span>
              <input
                id="calc-gross"
                type="number"
                step="0.01"
                min="0"
                value={calcGrossAmount}
                onChange={(e) => setCalcGrossAmount(e.target.value)}
                style={{ ...inputStyle, width: '8rem' }}
              />
            </div>
          </div>

          {/* Consumption mode selector */}
          <div>
            <fieldset style={{ border: 'none', padding: 0, margin: 0 }}>
              <legend style={labelStyle}>
                {/* Mode selection — no separate label needed, options are self-explanatory */}
              </legend>
              <div style={{ display: 'flex', gap: '1rem', marginTop: '0.25rem' }}>
                {CONSUMPTION_MODES.map((mode) => (
                  <label
                    key={mode}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.375rem',
                      fontSize: '0.875rem',
                      cursor: 'pointer',
                    }}
                  >
                    <input
                      type="radio"
                      name="calc-mode"
                      value={mode}
                      checked={calcMode === mode}
                      onChange={() => setCalcMode(mode)}
                    />
                    {t(`admin.taxConfiguration.consumptionModes.${mode}`)}
                  </label>
                ))}
              </div>
            </fieldset>
          </div>
        </div>

        {/* Breakdown result */}
        {breakdown && (
          <div
            style={{
              padding: '1rem',
              border: '1px solid #e5e7eb',
              borderRadius: '0.375rem',
              background: '#f9fafb',
            }}
          >
            <BreakdownRow
              label={t('admin.taxConfiguration.netAmount')}
              amount={breakdown.netAmount}
            />
            <BreakdownRow
              label={`${t('admin.taxConfiguration.vatAmount')} (${rates[calcMode]}%)`}
              amount={breakdown.vatAmount}
            />
            <BreakdownRow
              label={t('admin.taxConfiguration.grossAmount')}
              amount={breakdown.grossAmount}
              isBold
            />
          </div>
        )}
      </section>
    </main>
  );
}

// ---------------------------------------------------------------------------
// Sub-components
// ---------------------------------------------------------------------------

interface BreakdownRowProps {
  label: string;
  amount: number;
  isBold?: boolean;
}

function BreakdownRow({ label, amount, isBold = false }: BreakdownRowProps) {
  return (
    <div
      style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        padding: '0.25rem 0',
        borderTop: isBold ? '1px solid #e5e7eb' : undefined,
        marginTop: isBold ? '0.25rem' : undefined,
        paddingTop: isBold ? '0.5rem' : undefined,
      }}
    >
      <span
        style={{
          fontSize: '0.875rem',
          color: isBold ? '#111827' : '#6b7280',
          fontWeight: isBold ? 600 : 400,
        }}
      >
        {label}
      </span>
      <span
        style={{
          fontSize: '0.875rem',
          fontFamily: 'monospace',
          fontWeight: isBold ? 700 : 400,
          color: isBold ? '#111827' : '#374151',
        }}
      >
        € {amount.toFixed(2)}
      </span>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Style helpers
// ---------------------------------------------------------------------------

const sectionStyle: React.CSSProperties = {
  marginBottom: '2rem',
};

const sectionHeadingStyle: React.CSSProperties = {
  fontSize: '1rem',
  fontWeight: 700,
  marginBottom: '0.75rem',
  paddingBottom: '0.5rem',
  borderBottom: '1px solid #e5e7eb',
};

const labelStyle: React.CSSProperties = {
  display: 'block',
  fontWeight: 600,
  fontSize: '0.875rem',
  marginBottom: '0.125rem',
};

const inputStyle: React.CSSProperties = {
  width: '100%',
  padding: '0.5rem 0.75rem',
  border: '1px solid #d1d5db',
  borderRadius: '0.375rem',
  fontSize: '0.875rem',
  boxSizing: 'border-box',
};
