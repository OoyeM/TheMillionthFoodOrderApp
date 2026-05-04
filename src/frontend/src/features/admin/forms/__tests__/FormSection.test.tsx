import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { FormSection } from '../FormSection';

describe('FormSection', () => {
  it('renders title, description, and children when open', () => {
    render(
      <FormSection title="Basic info" description="Set the basics">
        <div>Inner content</div>
      </FormSection>,
    );

    expect(screen.getByText('Basic info')).toBeInTheDocument();
    expect(screen.getByText('Set the basics')).toBeInTheDocument();
    expect(screen.getByText('Inner content')).toBeInTheDocument();
  });

  it('hides children when collapsed', async () => {
    const user = userEvent.setup();
    render(
      <FormSection title="Group" defaultOpen>
        <div data-testid="inner">Inner</div>
      </FormSection>,
    );

    expect(screen.getByTestId('inner')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /Group/ }));
    expect(screen.queryByTestId('inner')).not.toBeInTheDocument();
  });

  it('starts collapsed when defaultOpen is false', () => {
    render(
      <FormSection title="Group" defaultOpen={false}>
        <div data-testid="inner">Inner</div>
      </FormSection>,
    );

    expect(screen.queryByTestId('inner')).not.toBeInTheDocument();
  });
});
