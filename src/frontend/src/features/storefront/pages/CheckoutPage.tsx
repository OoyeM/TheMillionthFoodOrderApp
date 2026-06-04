import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useAuth } from '@/auth/useAuth';
import { CartProvider, useCart } from '../context/CartContext';
import { useResolvedShop } from '../hooks/useResolvedShop';
import { useCreateOrder } from '../hooks/useCreateOrder';
import { MockPaymentScreen } from '../components/MockPaymentScreen';
import type { OrderType } from '@api/orders';
import type { SupportedLocale, EatInSettings } from '@/types/common';

// ---------------------------------------------------------------------------
// Language normalisation helper
// ---------------------------------------------------------------------------

const SUPPORTED_LOCALES: ReadonlySet<string> = new Set<string>(['nl', 'fr', 'de']);

/**
 * Maps a raw route :lang param to a supported locale.
 * Falls back to 'nl' for any unsupported value.
 */
function normalizeLang(lang: string | undefined): SupportedLocale {
  const code = lang?.toLowerCase();
  return code && SUPPORTED_LOCALES.has(code) ? (code as SupportedLocale) : 'nl';
}

// ---------------------------------------------------------------------------
// Zod schema factory (auth-aware validation)
// ---------------------------------------------------------------------------

/**
 * Builds the checkout schema based on the current auth state.
 *
 * - Guest (not authenticated): first name, last name, email, phone are all REQUIRED.
 * - Authenticated: fields come from the profile and are pre-filled; schema stays
 *   permissive because the form renders them read-only (still submitted).
 */
function makeCheckoutSchema(
  isAuthenticated: boolean,
  t: (key: string) => string,
  eatIn: EatInSettings,
) {
  // Guest checkout requires the full contact record; authenticated customers have it pre-filled
  // from their profile (still submitted), so only phone is independently required.
  const contactFields = isAuthenticated
    ? {
        customerFirstName: z.string().trim().min(1),
        customerLastName: z.string().trim().min(1),
        customerEmail: z
          .string()
          .trim()
          .refine((v) => !v || z.string().email().safeParse(v).success, {
            message: t('storefront.checkout.customerEmailInvalid'),
          }),
        customerPhone: z.string().trim().min(1, t('storefront.checkout.customerPhoneRequired')),
      }
    : {
        customerFirstName: z.string().trim().min(1, t('storefront.checkout.customerFirstNameRequired')),
        customerLastName: z.string().trim().min(1, t('storefront.checkout.customerLastNameRequired')),
        customerEmail: z
          .string()
          .trim()
          .min(1, t('storefront.checkout.customerEmailRequired'))
          .refine((v) => z.string().email().safeParse(v).success, {
            message: t('storefront.checkout.customerEmailInvalid'),
          }),
        customerPhone: z.string().trim().min(1, t('storefront.checkout.customerPhoneRequired')),
      };

  return z
    .object({
      orderType: z.enum(['Pickup', 'EatIn', 'Delivery']),
      ...contactFields,
      tableNumber: z.string(),
      paymentMethod: z.enum(['CashAtPickup', 'CreditCard', 'Bancontact']),
    })
    // Eat-in is only an allowed order type when the shop accepts it (US-FP-066).
    .refine((v) => eatIn.isEnabled || v.orderType !== 'EatIn', {
      message: t('storefront.checkout.eatInUnavailable'),
      path: ['orderType'],
    })
    .refine(
      (v) =>
        !(eatIn.requiresTableNumber && v.orderType === 'EatIn') ||
        v.tableNumber.trim().length > 0,
      { message: t('storefront.checkout.tableNumberRequired'), path: ['tableNumber'] },
    )
    .refine(
      (v) => {
        if (v.orderType !== 'EatIn' || v.tableNumber.trim().length === 0) return true;
        const parsed = Number(v.tableNumber);
        return Number.isInteger(parsed) && parsed > 0;
      },
      { message: t('storefront.checkout.tableNumberInvalid'), path: ['tableNumber'] },
    );
}

type CheckoutFormValues = {
  orderType: 'Pickup' | 'EatIn' | 'Delivery';
  customerFirstName: string;
  customerLastName: string;
  customerEmail: string;
  customerPhone: string;
  tableNumber: string;
  paymentMethod: 'CashAtPickup' | 'CreditCard' | 'Bancontact';
};

// ---------------------------------------------------------------------------
// Checkout form inner component (needs CartProvider to be above)
// ---------------------------------------------------------------------------

interface CheckoutFormProps {
  brandSlug: string;
  shopId: string;
  shopSlug: string;
  shopIsOpen: boolean;
  eatIn: EatInSettings;
}

function CheckoutForm({ brandSlug, shopId, shopSlug, shopIsOpen, eatIn }: CheckoutFormProps) {
  const { t } = useTranslation('common');
  const navigate = useNavigate();
  const { brandSlug: paramSlug, lang } = useParams<{ brandSlug: string; lang: string }>();
  const { user, isAuthenticated } = useAuth();
  const { state, clearCart } = useCart();
  const createOrder = useCreateOrder(brandSlug, shopId);

  const [showMockPayment, setShowMockPayment] = useState(false);
  const [pendingOrderId, setPendingOrderId] = useState<string | null>(null);

  // Redirect to menu if cart is empty
  useEffect(() => {
    if (state.items.length === 0) {
      void navigate(`/${paramSlug}/${lang}/${shopSlug}/menu`, { replace: true });
    }
  }, [state.items.length, navigate, paramSlug, lang, shopSlug]);

  // Build auth-aware schema (memoised so it only rebuilds when auth state or t changes)
  const schema = useMemo(
    () => makeCheckoutSchema(isAuthenticated, t, eatIn),
    [isAuthenticated, t, eatIn],
  );

  const defaultValues: CheckoutFormValues = {
    orderType: 'Pickup',
    customerFirstName: user?.firstName ?? '',
    customerLastName: user?.lastName ?? '',
    customerEmail: user?.email ?? '',
    customerPhone: user?.phoneNumber ?? '',
    tableNumber: '',
    paymentMethod: 'CashAtPickup',
  };

  const {
    register,
    handleSubmit,
    watch,
    control,
    formState: { errors, isSubmitting },
  } = useForm<CheckoutFormValues>({
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    resolver: zodResolver(schema as z.ZodType<CheckoutFormValues, any, any>),
    defaultValues,
  });

  const orderType = watch('orderType');
  const orderTypeOptions = eatIn.isEnabled
    ? (['Pickup', 'EatIn', 'Delivery'] as const)
    : (['Pickup', 'Delivery'] as const);

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
    // Guard: a closed shop does not accept online orders (mirrors the backend check).
    if (!shopIsOpen) return;

    const orderItems = state.items.map((item) => ({
      productId: item.productId,
      quantity: item.quantity,
      selectedModifierIds: item.selectedModifiers.map((m) => m.modifierId),
    }));

    const result = await createOrder.mutateAsync({
      orderType: values.orderType as OrderType,
      customerFirstName: values.customerFirstName || null,
      customerLastName: values.customerLastName || null,
      customerEmail: values.customerEmail || null,
      customerPhone: values.customerPhone || null,
      items: orderItems,
      paymentMethod: values.paymentMethod,
      languageCode: normalizeLang(lang),
      tableNumber:
        values.orderType === 'EatIn' && values.tableNumber.trim().length > 0
          ? Number(values.tableNumber)
          : null,
    });

    clearCart();

    if (values.paymentMethod === 'CashAtPickup') {
      void navigate(`/${paramSlug}/${lang}/${shopSlug}/order/${result.id}`);
    } else {
      // Online payment methods: show mock processing screen first
      setPendingOrderId(result.id);
      setShowMockPayment(true);
    }
  }

  function handleMockPaymentComplete() {
    setShowMockPayment(false);
    void navigate(`/${paramSlug}/${lang}/${shopSlug}/order/${pendingOrderId}`);
  }

  if (state.items.length === 0) {
    return null; // Redirecting
  }

  if (showMockPayment && pendingOrderId) {
    return <MockPaymentScreen orderId={pendingOrderId} onComplete={handleMockPaymentComplete} />;
  }

  const isReadOnlyField = isAuthenticated;

  return (
    <main style={{ maxWidth: '36rem', margin: '0 auto', padding: '1.5rem 1rem' }}>
      <h1 style={{ fontSize: '1.75rem', fontWeight: 800, color: '#111827', marginBottom: '1.5rem' }}>
        {t('storefront.checkout.title')}
      </h1>

      {!shopIsOpen && (
        <div
          role="alert"
          style={{
            padding: '0.75rem 1rem',
            borderRadius: '0.375rem',
            background: '#fef2f2',
            border: '1px solid #fecaca',
            color: '#991b1b',
            fontSize: '0.875rem',
            marginBottom: '1.5rem',
          }}
        >
          {t('storefront.checkout.shopClosed')}
        </div>
      )}

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
                {orderTypeOptions.map((type) => (
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

        {/* Table number — shown for eat-in; required when the shop mandates it (US-FP-066) */}
        {orderType === 'EatIn' && (
          <div style={{ marginBottom: '1.5rem' }}>
            <label
              htmlFor="tableNumber"
              style={{
                display: 'block',
                fontSize: '0.9375rem',
                fontWeight: 600,
                color: '#374151',
                marginBottom: '0.375rem',
              }}
            >
              {t('storefront.checkout.tableNumberLabel')}
              {eatIn.requiresTableNumber && (
                <span style={{ color: '#ef4444', marginLeft: '0.25rem' }}>*</span>
              )}
            </label>
            <input
              id="tableNumber"
              type="number"
              min={1}
              inputMode="numeric"
              {...register('tableNumber')}
              placeholder={t('storefront.checkout.tableNumberPlaceholder')}
              style={{
                width: '100%',
                padding: '0.625rem 0.875rem',
                borderRadius: '0.375rem',
                border: `1px solid ${errors.tableNumber ? '#ef4444' : '#d1d5db'}`,
                fontSize: '0.9375rem',
                color: '#111827',
                boxSizing: 'border-box',
              }}
            />
            {errors.tableNumber && (
              <p style={{ color: '#ef4444', fontSize: '0.8125rem', marginTop: '0.25rem' }}>
                {errors.tableNumber.message}
              </p>
            )}
          </div>
        )}

        {/* Customer first name */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label
            htmlFor="customerFirstName"
            style={{
              display: 'block',
              fontSize: '0.9375rem',
              fontWeight: 600,
              color: '#374151',
              marginBottom: '0.375rem',
            }}
          >
            {t('storefront.checkout.customerFirstNameLabel')}
            {!isAuthenticated && (
              <span style={{ color: '#ef4444', marginLeft: '0.25rem' }}>*</span>
            )}
          </label>
          <input
            id="customerFirstName"
            type="text"
            {...register('customerFirstName')}
            placeholder={t('storefront.checkout.customerFirstNamePlaceholder')}
            readOnly={isReadOnlyField}
            disabled={isReadOnlyField}
            style={{
              width: '100%',
              padding: '0.625rem 0.875rem',
              borderRadius: '0.375rem',
              border: `1px solid ${errors.customerFirstName ? '#ef4444' : '#d1d5db'}`,
              fontSize: '0.9375rem',
              color: '#111827',
              boxSizing: 'border-box',
              background: isReadOnlyField ? '#f9fafb' : '#fff',
            }}
          />
          {errors.customerFirstName && (
            <p style={{ color: '#ef4444', fontSize: '0.8125rem', marginTop: '0.25rem' }}>
              {errors.customerFirstName.message}
            </p>
          )}
        </div>

        {/* Customer last name */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label
            htmlFor="customerLastName"
            style={{
              display: 'block',
              fontSize: '0.9375rem',
              fontWeight: 600,
              color: '#374151',
              marginBottom: '0.375rem',
            }}
          >
            {t('storefront.checkout.customerLastNameLabel')}
            {!isAuthenticated && (
              <span style={{ color: '#ef4444', marginLeft: '0.25rem' }}>*</span>
            )}
          </label>
          <input
            id="customerLastName"
            type="text"
            {...register('customerLastName')}
            placeholder={t('storefront.checkout.customerLastNamePlaceholder')}
            readOnly={isReadOnlyField}
            disabled={isReadOnlyField}
            style={{
              width: '100%',
              padding: '0.625rem 0.875rem',
              borderRadius: '0.375rem',
              border: `1px solid ${errors.customerLastName ? '#ef4444' : '#d1d5db'}`,
              fontSize: '0.9375rem',
              color: '#111827',
              boxSizing: 'border-box',
              background: isReadOnlyField ? '#f9fafb' : '#fff',
            }}
          />
          {errors.customerLastName && (
            <p style={{ color: '#ef4444', fontSize: '0.8125rem', marginTop: '0.25rem' }}>
              {errors.customerLastName.message}
            </p>
          )}
        </div>

        {/* Customer email */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label
            htmlFor="customerEmail"
            style={{
              display: 'block',
              fontSize: '0.9375rem',
              fontWeight: 600,
              color: '#374151',
              marginBottom: '0.375rem',
            }}
          >
            {t('storefront.checkout.customerEmailLabel')}
            {!isAuthenticated ? (
              <span style={{ color: '#ef4444', marginLeft: '0.25rem' }}>*</span>
            ) : null}
          </label>
          <input
            id="customerEmail"
            type="email"
            {...register('customerEmail')}
            placeholder={t('storefront.checkout.customerEmailPlaceholder')}
            readOnly={isReadOnlyField}
            disabled={isReadOnlyField}
            style={{
              width: '100%',
              padding: '0.625rem 0.875rem',
              borderRadius: '0.375rem',
              border: `1px solid ${errors.customerEmail ? '#ef4444' : '#d1d5db'}`,
              fontSize: '0.9375rem',
              color: '#111827',
              boxSizing: 'border-box',
              background: isReadOnlyField ? '#f9fafb' : '#fff',
            }}
          />
          {errors.customerEmail && (
            <p style={{ color: '#ef4444', fontSize: '0.8125rem', marginTop: '0.25rem' }}>
              {errors.customerEmail.message}
            </p>
          )}
          <p style={{ color: '#6b7280', fontSize: '0.8125rem', marginTop: '0.25rem' }}>
            {t('storefront.checkout.customerEmailHelper')}
          </p>
        </div>

        {/* Customer phone — always editable + required */}
        <div style={{ marginBottom: '1.5rem' }}>
          <label
            htmlFor="customerPhone"
            style={{
              display: 'block',
              fontSize: '0.9375rem',
              fontWeight: 600,
              color: '#374151',
              marginBottom: '0.375rem',
            }}
          >
            {t('storefront.checkout.customerPhoneLabel')}
            <span style={{ color: '#ef4444', marginLeft: '0.25rem' }}>*</span>
          </label>
          <input
            id="customerPhone"
            type="tel"
            {...register('customerPhone')}
            placeholder={t('storefront.checkout.customerPhonePlaceholder')}
            style={{
              width: '100%',
              padding: '0.625rem 0.875rem',
              borderRadius: '0.375rem',
              border: `1px solid ${errors.customerPhone ? '#ef4444' : '#d1d5db'}`,
              fontSize: '0.9375rem',
              color: '#111827',
              boxSizing: 'border-box',
            }}
          />
          {errors.customerPhone && (
            <p style={{ color: '#ef4444', fontSize: '0.8125rem', marginTop: '0.25rem' }}>
              {errors.customerPhone.message}
            </p>
          )}
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
          disabled={isSubmitting || createOrder.isPending || !shopIsOpen}
          style={{
            width: '100%',
            padding: '0.875rem',
            borderRadius: '0.5rem',
            border: 'none',
            background: 'var(--brand-color-primary, #111827)',
            color: '#fff',
            fontWeight: 700,
            fontSize: '1rem',
            cursor: isSubmitting || createOrder.isPending || !shopIsOpen ? 'not-allowed' : 'pointer',
            opacity: isSubmitting || createOrder.isPending || !shopIsOpen ? 0.7 : 1,
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
// CheckoutPage — reads shop from ShopContext, wraps form in CartProvider
// ---------------------------------------------------------------------------

export function CheckoutPage() {
  const { brandSlug } = useParams<{ brandSlug: string }>();
  const { t } = useTranslation('common');
  // shopId and shopSlug come from the resolved ShopContext (set by ShopResolver).
  const shop = useResolvedShop();
  const resolvedBrandSlug = brandSlug ?? '';

  if (!shop.id) {
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
    <CartProvider brandSlug={resolvedBrandSlug} shopId={shop.id}>
      <CheckoutForm
        brandSlug={resolvedBrandSlug}
        shopId={shop.id}
        shopSlug={shop.slug}
        shopIsOpen={shop.isOpen}
        eatIn={shop.eatIn}
      />
    </CartProvider>
  );
}
