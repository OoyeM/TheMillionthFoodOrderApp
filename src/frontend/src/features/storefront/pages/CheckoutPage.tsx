import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '@/auth/useAuth';
import { CartProvider, useCart } from '../context/CartContext';
import { useCreateOrder } from '../hooks/useCreateOrder';
import { MockPaymentScreen } from '../components/MockPaymentScreen';
import type { OrderType } from '@api/orders';

// ---------------------------------------------------------------------------
// Zod schema
// ---------------------------------------------------------------------------

const checkoutSchema = z.object({
  orderType: z.enum(['Pickup', 'EatIn', 'Delivery']),
  customerName: z.string().trim().optional(),
  paymentMethod: z.enum(['CashAtPickup', 'CreditCard', 'Bancontact']),
});

type CheckoutFormValues = z.infer<typeof checkoutSchema>;

// ---------------------------------------------------------------------------
// Checkout form inner component (needs CartProvider to be above)
// ---------------------------------------------------------------------------

interface CheckoutFormProps {
  brandSlug: string;
  shopId: string;
}

function CheckoutForm({ brandSlug, shopId }: CheckoutFormProps) {
  const { t } = useTranslation('common');
  const navigate = useNavigate();
  const { brandSlug: paramSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const { user } = useAuth();
  const { state, clearCart } = useCart();
  const createOrder = useCreateOrder(brandSlug, shopId);

  const [showMockPayment, setShowMockPayment] = useState(false);
  const [pendingOrderId, setPendingOrderId] = useState<string | null>(null);

  // Redirect to menu if cart is empty
  useEffect(() => {
    if (state.items.length === 0) {
      void navigate(`/${paramSlug}/${lang}/shops/${shopId}/menu`, { replace: true });
    }
  }, [state.items.length, navigate, paramSlug, lang, shopId]);

  const {
    register,
    handleSubmit,
    watch,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CheckoutFormValues>({
    resolver: zodResolver(checkoutSchema),
    defaultValues: {
      customerName: user?.displayName ?? '',
    },
  });

  const orderType = watch('orderType');

  const vatNotice =
    orderType === 'EatIn'
      ? t('storefront.checkout.vatEatIn')
      : orderType
        ? t('storefront.checkout.vatTakeaway')
        : null;

  function formatCurrency(amount: number): string {
    return new Intl.NumberFormat('nl-BE', { style: 'currency', currency: 'EUR' }).format(amount);
  }

  const cartSubtotal = state.items.reduce((sum, item) => {
    const modTotal = item.selectedModifiers.reduce((s, m) => s + m.priceAdjustment, 0);
    return sum + item.quantity * (item.unitGrossPrice + modTotal);
  }, 0);

  async function onSubmit(values: CheckoutFormValues) {
    const orderItems = state.items.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
      selectedModifierIds: item.selectedModifiers.map((m) => m.modifierId),
    }));

    const result = await createOrder.mutateAsync({
      orderType: values.orderType as OrderType,
      customerName: values.customerName ?? null,
      items: orderItems,
      paymentMethod: values.paymentMethod,
    });

    // Save the shopId mapping so OrderConfirmationPage can reconstruct the API route
    sessionStorage.setItem(`order-shop:${brandSlug}:${result.id}`, result.shopId);
    clearCart();

    if (values.paymentMethod === 'CashAtPickup') {
      void navigate(`/${paramSlug}/${lang}/order/${result.id}`);
    } else {
      // Online payment methods: show mock processing screen first
      setPendingOrderId(result.id);
      setShowMockPayment(true);
    }
  }

  function handleMockPaymentComplete() {
    setShowMockPayment(false);
    void navigate(`/${paramSlug}/${lang}/order/${pendingOrderId}`);
  }

  if (state.items.length === 0) {
    return null; // Redirecting
  }

  if (showMockPayment && pendingOrderId) {
    return <MockPaymentScreen orderId={pendingOrderId} onComplete={handleMockPaymentComplete} />;
  }

  return (
    <main style={{ maxWidth: '36rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, color: '#111827', marginBottom: '1.5rem' }}>
        {t('storefront.checkout.title')}
      </h1>

      <form onSubmit={(e) => { void handleSubmit(onSubmit)(e); }} noValidate>
        {/* Order type selection */}
        <fieldset
          style={{
            border: 'none',
            padding: 0,
            margin: '0 0 1.5rem',
          }}
        >
          <legend
            style={{
              fontSize: '1rem',
              fontWeight: 700,
              color: '#111827',
              marginBottom: '0.75rem',
            }}
          >
            {t('storefront.checkout.orderTypeLabel')}
            <span style={{ color: '#ef4444', marginLeft: '0.25rem' }}>*</span>
          </legend>

          {errors.orderType && (
            <p style={{ color: '#ef4444', fontSize: '0.875rem', marginBottom: '0.5rem' }}>
              {errors.orderType.message}
            </p>
          )}

          <Controller
            name="orderType"
            control={control}
            render={({ field }) => (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                {(['Pickup', 'EatIn', 'Delivery'] as const).map((type) => (
                  <label
                    key={type}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.75rem',
                      padding: '0.875rem 1rem',
                      borderRadius: '0.5rem',
                      border: `2px solid ${field.value === type ? 'var(--brand-color-primary, #111827)' : '#e5e7eb'}`,
                      background: field.value === type ? '#f9fafb' : '#fff',
                      cursor: 'pointer',
                      fontSize: '0.9375rem',
                      fontWeight: field.value === type ? 600 : 400,
                      color: '#111827',
                    }}
                  >
                    <input
                      type="radio"
                      value={type}
                      checked={field.value === type}
                      onChange={() => field.onChange(type)}
                      style={{ width: '1.125rem', height: '1.125rem', cursor: 'pointer' }}
                    />
                    {t(`storefront.checkout.orderType.${type}`)}
                  </label>
                ))}
              </div>
            )}
          />
        </fieldset>

        {/* VAT notice */}
        {vatNotice && (
          <div
            style={{
              padding: '0.75rem 1rem',
              borderRadius: '0.375rem',
              background: '#eff6ff',
              border: '1px solid #bfdbfe',
              color: '#1e40af',
              fontSize: '0.875rem',
              marginBottom: '1.5rem',
            }}
          >
            {vatNotice}
          </div>
        )}

        {/* Customer name */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label
            htmlFor="customerName"
            style={{
              display: 'block',
              fontSize: '0.9375rem',
              fontWeight: 600,
              color: '#374151',
              marginBottom: '0.375rem',
            }}
          >
            {t('storefront.checkout.customerNameLabel')}
            <span style={{ fontWeight: 400, color: '#6b7280', marginLeft: '0.25rem' }}>
              ({t('storefront.checkout.optional')})
            </span>
          </label>
          <input
            id="customerName"
            type="text"
            {...register('customerName')}
            placeholder={t('storefront.checkout.customerNamePlaceholder')}
            style={{
              width: '100%',
              padding: '0.625rem 0.875rem',
              borderRadius: '0.375rem',
              border: '1px solid #d1d5db',
              fontSize: '0.9375rem',
              color: '#111827',
              boxSizing: 'border-box',
            }}
          />
        </div>

        {/* Cart summary */}
        <div
          style={{
            border: '1px solid #e5e7eb',
            borderRadius: '0.5rem',
            overflow: 'hidden',
            marginBottom: '1.5rem',
          }}
        >
          <div
            style={{
              padding: '0.75rem 1rem',
              background: '#f9fafb',
              borderBottom: '1px solid #e5e7eb',
              fontWeight: 700,
              fontSize: '0.9375rem',
              color: '#111827',
            }}
          >
            {t('storefront.checkout.orderSummary')}
          </div>
          <div style={{ padding: '0.75rem 1rem' }}>
            {state.items.map((item) => {
              const modTotal = item.selectedModifiers.reduce((s, m) => s + m.priceAdjustment, 0);
              const lineTotal = item.quantity * (item.unitGrossPrice + modTotal);
              const key = `${item.productId}-${item.selectedModifiers.map((m) => m.modifierId).join(',')}`;
              return (
                <div
                  key={key}
                  style={{
                    display: 'flex',
                    justifyContent: 'space-between',
                    gap: '1rem',
                    padding: '0.375rem 0',
                    fontSize: '0.9375rem',
                    color: '#374151',
                  }}
                >
                  <span>
                    {item.quantity}&times; {item.productName}
                    {item.selectedModifiers.length > 0 && (
                      <span style={{ fontSize: '0.8125rem', color: '#6b7280', display: 'block' }}>
                        {item.selectedModifiers.map((m) => m.modifierName).join(', ')}
                      </span>
                    )}
                  </span>
                  <span style={{ whiteSpace: 'nowrap', fontWeight: 600 }}>
                    {formatCurrency(lineTotal)}
                  </span>
                </div>
              );
            })}
            <div
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                marginTop: '0.75rem',
                paddingTop: '0.75rem',
                borderTop: '1px solid #e5e7eb',
                fontWeight: 700,
                fontSize: '1rem',
              }}
            >
              <span>{t('storefront.checkout.subtotal')}</span>
              <span>{formatCurrency(cartSubtotal)}</span>
            </div>
          </div>
        </div>

        {/* Payment method selection */}
        <fieldset
          style={{
            border: 'none',
            padding: 0,
            margin: '0 0 1.5rem',
          }}
        >
          <legend
            style={{
              fontSize: '1rem',
              fontWeight: 700,
              color: '#111827',
              marginBottom: '0.75rem',
            }}
          >
            {t('storefront.checkout.payment.label')}
            <span style={{ color: '#ef4444', marginLeft: '0.25rem' }}>*</span>
          </legend>

          {errors.paymentMethod && (
            <p style={{ color: '#ef4444', fontSize: '0.875rem', marginBottom: '0.5rem' }}>
              {errors.paymentMethod.message}
            </p>
          )}

          <Controller
            name="paymentMethod"
            control={control}
            render={({ field }) => (
              <div style={{ display: 'flex', flexDirection: 'column', gap: '0.5rem' }}>
                {(['CashAtPickup', 'CreditCard', 'Bancontact'] as const).map((method) => (
                  <label
                    key={method}
                    style={{
                      display: 'flex',
                      alignItems: 'center',
                      gap: '0.75rem',
                      padding: '0.875rem 1rem',
                      borderRadius: '0.5rem',
                      border: `2px solid ${field.value === method ? 'var(--brand-color-primary, #111827)' : '#e5e7eb'}`,
                      background: field.value === method ? '#f9fafb' : '#fff',
                      cursor: 'pointer',
                      fontSize: '0.9375rem',
                      fontWeight: field.value === method ? 600 : 400,
                      color: '#111827',
                    }}
                  >
                    <input
                      type="radio"
                      value={method}
                      checked={field.value === method}
                      onChange={() => field.onChange(method)}
                      style={{ width: '1.125rem', height: '1.125rem', cursor: 'pointer' }}
                    />
                    {t(`storefront.checkout.payment.${method === 'CashAtPickup' ? 'cashAtPickup' : method === 'CreditCard' ? 'creditCard' : 'bancontact'}`)}
                  </label>
                ))}
              </div>
            )}
          />
        </fieldset>

        {/* Error */}
        {createOrder.isError && (
          <div
            style={{
              padding: '0.75rem 1rem',
              borderRadius: '0.375rem',
              background: '#fef2f2',
              border: '1px solid #fecaca',
              color: '#991b1b',
              fontSize: '0.875rem',
              marginBottom: '1rem',
            }}
          >
            {t('storefront.checkout.submitError')}
          </div>
        )}

        {/* Submit */}
        <button
          type="submit"
          disabled={isSubmitting || createOrder.isPending}
          style={{
            width: '100%',
            padding: '0.875rem',
            borderRadius: '0.5rem',
            border: 'none',
            background: 'var(--brand-color-primary, #111827)',
            color: '#fff',
            fontWeight: 700,
            fontSize: '1rem',
            cursor: isSubmitting || createOrder.isPending ? 'not-allowed' : 'pointer',
            opacity: isSubmitting || createOrder.isPending ? 0.7 : 1,
          }}
        >
          {isSubmitting || createOrder.isPending
            ? t('storefront.checkout.placing')
            : t('storefront.checkout.placeOrder')}
        </button>
      </form>
    </main>
  );
}

// ---------------------------------------------------------------------------
// CheckoutPage — wraps form in CartProvider
// ---------------------------------------------------------------------------

export function CheckoutPage() {
  const { brandSlug } = useParams<{ brandSlug: string }>();
  const resolvedBrandSlug = brandSlug ?? '';

  return <CheckoutPageInner brandSlug={resolvedBrandSlug} />;
}

/**
 * Inner component that creates a CartProvider by reading the shopId from localStorage
 * directly (before CartContext is available). This avoids a chicken-and-egg problem.
 */
function CheckoutPageInner({ brandSlug }: { brandSlug: string }) {
  const { t } = useTranslation('common');

  // Find the first cart key in localStorage for this brand
  const shopId = findActiveShopId(brandSlug);

  if (!shopId) {
    return (
      <main style={{ maxWidth: '36rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
        <h1 style={{ fontSize: '1.75rem', fontWeight: 800, color: '#111827', marginBottom: '1rem' }}>
          {t('storefront.checkout.title')}
        </h1>
        <p style={{ color: '#6b7280' }}>{t('storefront.checkout.cartEmpty')}</p>
      </main>
    );
  }

  return (
    <CartProvider brandSlug={brandSlug} shopId={shopId}>
      <CheckoutForm brandSlug={brandSlug} shopId={shopId} />
    </CartProvider>
  );
}

/**
 * Scans localStorage for a cart belonging to the given brandSlug.
 * Returns the first shopId found, or null if no cart exists.
 */
function findActiveShopId(brandSlug: string): string | null {
  const prefix = `cart:${brandSlug}:`;
  for (let i = 0; i < localStorage.length; i++) {
    const key = localStorage.key(i);
    if (key?.startsWith(prefix)) {
      try {
        const raw = localStorage.getItem(key);
        if (!raw) continue;
        const parsed = JSON.parse(raw) as { shopId?: string; items?: unknown[] };
        if (parsed.shopId && Array.isArray(parsed.items) && parsed.items.length > 0) {
          return parsed.shopId;
        }
      } catch {
        continue;
      }
    }
  }
  return null;
}
