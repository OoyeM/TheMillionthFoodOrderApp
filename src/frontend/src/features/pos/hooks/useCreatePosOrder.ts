import { useMutation } from '@tanstack/react-query';
import { useNavigate, useParams } from 'react-router-dom';
import { ordersApi } from '@api/orders';
import type { CreateOrderRequest, OrderResponse } from '@api/orders';
import type { PosOrderAction } from './usePosOrder';

/**
 * Mutation hook for submitting a POS order.
 * On success, dispatches a RESET to clear the ticket and navigates to the
 * order confirmation page.
 *
 * All POS orders use PaymentMethod=CashAtPickup per US-FP-018 scope decision.
 */
export function useCreatePosOrder(
  dispatch: React.Dispatch<PosOrderAction>,
) {
  const navigate = useNavigate();
  const { brandSlug, lang, shopId } = useParams<{
    brandSlug: string;
    lang: string;
    shopId: string;
  }>();

  const resolvedBrand = brandSlug ?? '';
  const resolvedShop = shopId ?? '';
  const resolvedLang = lang ?? 'nl';

  return useMutation<OrderResponse, Error, CreateOrderRequest>({
    mutationFn: (data: CreateOrderRequest) =>
      ordersApi.create(resolvedBrand, resolvedShop, data),
    onSuccess: (order) => {
      dispatch({ type: 'RESET' });
      navigate(
        `/${resolvedBrand}/${resolvedLang}/pos/shops/${resolvedShop}/order/confirmation/${order.id}`,
      );
    },
  });
}
