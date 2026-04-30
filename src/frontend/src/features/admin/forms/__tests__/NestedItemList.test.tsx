import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { useForm } from 'react-hook-form';
import { NestedItemList } from '../NestedItemList';

interface ItemForm {
  items: { value: string }[];
}

function Harness({ initial = [] }: { initial?: { value: string }[] }) {
  const form = useForm<ItemForm>({ defaultValues: { items: initial } });
  return (
    <NestedItemList<ItemForm, { value: string }>
      form={form}
      name="items"
      renderRow={(field, index, remove) => (
        <div key={field.id} data-testid={`row-${String(index)}`}>
          <span>{field.value}</span>
          <button type="button" onClick={remove} data-testid={`remove-${String(index)}`}>
            Remove
          </button>
        </div>
      )}
      newItem={() => ({ value: 'new' })}
      addLabel="Add item"
    />
  );
}

describe('NestedItemList', () => {
  it('renders existing rows', () => {
    render(<Harness initial={[{ value: 'a' }, { value: 'b' }]} />);
    expect(screen.getByTestId('row-0')).toHaveTextContent('a');
    expect(screen.getByTestId('row-1')).toHaveTextContent('b');
  });

  it('appends a new row when add is clicked', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    expect(screen.queryByTestId('row-0')).not.toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: 'Add item' }));
    expect(screen.getByTestId('row-0')).toHaveTextContent('new');
  });

  it('removes a row when remove is clicked', async () => {
    const user = userEvent.setup();
    render(<Harness initial={[{ value: 'a' }, { value: 'b' }]} />);

    expect(screen.getByTestId('row-1')).toBeInTheDocument();
    await user.click(screen.getByTestId('remove-0'));
    // After removal, the second row reindexes to 0
    expect(screen.getByTestId('row-0')).toHaveTextContent('b');
    expect(screen.queryByTestId('row-1')).not.toBeInTheDocument();
  });
});
