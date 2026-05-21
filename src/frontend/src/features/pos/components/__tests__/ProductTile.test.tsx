import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import '../../../../i18n/config';
import { ProductTile } from '../ProductTile';
import type { ProductListItem } from '@/types/common';

function makeProduct(overrides?: Partial<ProductListItem>): ProductListItem {
  return {
    id: 'p1',
    productType: 'Simple',
    name: 'Frietje Klein',
    basePrice: { amount: 3.5, currency: 'EUR' },
    imageUrl: null,
    menuCategoryId: null,
    sortOrderInCategory: 0,
    allergens: [],
    dietaryTags: [],
    createdAt: '2026-01-01T00:00:00Z',
    ...overrides,
  };
}

describe('ProductTile', () => {
  it('renders the product name', () => {
    const product = makeProduct({ name: 'Frietje Groot' });
    render(<ProductTile product={product} onTap={() => {}} />);
    expect(screen.getByText('Frietje Groot')).toBeInTheDocument();
  });

  it('renders the formatted price', () => {
    const product = makeProduct({ basePrice: { amount: 3.5, currency: 'EUR' } });
    render(<ProductTile product={product} onTap={() => {}} />);
    // Belgian locale formats 3.5 EUR as "€ 3,50" or "€3,50"
    expect(screen.getByText(/3,50/)).toBeInTheDocument();
  });

  it('calls onTap with the product when clicked', () => {
    const product = makeProduct();
    const handleTap = vi.fn();
    render(<ProductTile product={product} onTap={handleTap} />);
    fireEvent.click(screen.getByTestId(`product-tile-${product.id}`));
    expect(handleTap).toHaveBeenCalledOnce();
    expect(handleTap).toHaveBeenCalledWith(product);
  });

  it('has the expected data-testid', () => {
    const product = makeProduct({ id: 'test-id-123' });
    render(<ProductTile product={product} onTap={() => {}} />);
    expect(screen.getByTestId('product-tile-test-id-123')).toBeInTheDocument();
  });
});
