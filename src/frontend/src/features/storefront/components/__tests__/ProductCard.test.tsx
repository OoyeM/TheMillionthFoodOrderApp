import { beforeAll, describe, it, expect, vi } from 'vitest';
import { screen, render, within } from '@testing-library/react';
import i18next from 'i18next';
import { Allergen, DietaryTag } from '@/types/common';
import type { ProductListItem } from '@/types/common';
import { ProductCard } from '../ProductCard';
import '../../../../i18n/config';

beforeAll(async () => {
  await i18next.changeLanguage('nl');
});

function t(key: string, options?: { name: string }): string {
  return options ? i18next.t(key, options) : i18next.t(key);
}

function makeProduct(overrides: Partial<ProductListItem> = {}): ProductListItem {
  return {
    id: 'p-1',
    productType: 'Simple',
    name: 'Frietjes',
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

describe('ProductCard', () => {
  it('renders the product name and price', () => {
    render(<ProductCard product={makeProduct()} onAdd={vi.fn()} />);
    expect(screen.getByText('Frietjes')).toBeInTheDocument();
  });

  it('renders no tags section when product has no allergens or dietary tags', () => {
    render(<ProductCard product={makeProduct()} onAdd={vi.fn()} />);
    expect(screen.queryByTestId('product-tags')).toBeNull();
  });

  it('renders an allergen chip with the localised label', () => {
    render(
      <ProductCard
        product={makeProduct({ allergens: [Allergen.Gluten] })}
        onAdd={vi.fn()}
      />,
    );
    const tags = screen.getByTestId('product-tags');
    expect(within(tags).getByText(t('allergens.Gluten'))).toBeInTheDocument();
  });

  it('renders a dietary chip with the localised label', () => {
    render(
      <ProductCard
        product={makeProduct({ dietaryTags: [DietaryTag.Vegan] })}
        onAdd={vi.fn()}
      />,
    );
    const tags = screen.getByTestId('product-tags');
    expect(within(tags).getByText(t('dietaryTags.Vegan'))).toBeInTheDocument();
  });

  it('renders both allergen and dietary chips when both are present', () => {
    render(
      <ProductCard
        product={makeProduct({
          allergens: [Allergen.Nuts],
          dietaryTags: [DietaryTag.Vegetarian],
        })}
        onAdd={vi.fn()}
      />,
    );
    const tags = screen.getByTestId('product-tags');
    expect(within(tags).getByText(t('allergens.Nuts'))).toBeInTheDocument();
    expect(within(tags).getByText(t('dietaryTags.Vegetarian'))).toBeInTheDocument();
  });

  it('labels allergen chips with "Contains {name}" for screen readers', () => {
    render(
      <ProductCard
        product={makeProduct({ allergens: [Allergen.Milk] })}
        onAdd={vi.fn()}
      />,
    );
    const containsLabel = t('storefront.menu.contains', { name: t('allergens.Milk') });
    expect(screen.getByLabelText(containsLabel)).toBeInTheDocument();
  });
});
