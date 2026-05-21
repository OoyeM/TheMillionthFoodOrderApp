import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import '../../../../i18n/config';
import { OrderTypeSelector } from '../OrderTypeSelector';

describe('OrderTypeSelector', () => {
  it('renders Pickup and EatIn buttons', () => {
    render(<OrderTypeSelector value="Pickup" onChange={() => {}} />);
    expect(screen.getByRole('button', { name: /afhalen/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /ter plaatse/i })).toBeInTheDocument();
  });

  it('marks the active type as pressed', () => {
    render(<OrderTypeSelector value="EatIn" onChange={() => {}} />);
    const eatInBtn = screen.getByRole('button', { name: /ter plaatse/i });
    expect(eatInBtn).toHaveAttribute('aria-pressed', 'true');
    const pickupBtn = screen.getByRole('button', { name: /afhalen/i });
    expect(pickupBtn).toHaveAttribute('aria-pressed', 'false');
  });

  it('calls onChange with EatIn when the EatIn button is clicked', () => {
    const handleChange = vi.fn();
    render(<OrderTypeSelector value="Pickup" onChange={handleChange} />);
    fireEvent.click(screen.getByRole('button', { name: /ter plaatse/i }));
    expect(handleChange).toHaveBeenCalledWith('EatIn');
  });

  it('calls onChange with Pickup when the Pickup button is clicked', () => {
    const handleChange = vi.fn();
    render(<OrderTypeSelector value="EatIn" onChange={handleChange} />);
    fireEvent.click(screen.getByRole('button', { name: /afhalen/i }));
    expect(handleChange).toHaveBeenCalledWith('Pickup');
  });

  it('does NOT render a Delivery button', () => {
    render(<OrderTypeSelector value="Pickup" onChange={() => {}} />);
    expect(screen.queryByRole('button', { name: /bezorging/i })).not.toBeInTheDocument();
  });
});
