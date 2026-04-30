import { type ReactNode } from 'react';
import {
  useFieldArray,
  type ArrayPath,
  type FieldValues,
  type UseFormReturn,
} from 'react-hook-form';

interface NestedItemListProps<TFormValues extends FieldValues, TItem> {
  form: UseFormReturn<TFormValues>;
  /** Dotted path into the form values pointing at an array (e.g. 'componentProductIds'). */
  name: ArrayPath<TFormValues>;
  /** Renders a single row. Receives the field, its index, and a remove callback. */
  renderRow: (field: TItem & { id: string }, index: number, remove: () => void) => ReactNode;
  /** Returns the value to append when "Add" is clicked. */
  newItem: () => TItem;
  /** Optional label for the add button. Defaults to "Add". */
  addLabel?: string;
}

/**
 * Wraps RHF's useFieldArray to render a dynamic list of rows with
 * standardized add/remove controls. Used by combo product items,
 * modifier group options, and opening-hour slots.
 */
export function NestedItemList<TFormValues extends FieldValues, TItem>(
  props: NestedItemListProps<TFormValues, TItem>,
): JSX.Element {
  const { form, name, renderRow, newItem, addLabel = 'Add' } = props;
  const { fields, append, remove } = useFieldArray<TFormValues>({
    control: form.control,
    name,
  });

  return (
    <div className="nested-item-list">
      {fields.map((field, index) =>
        renderRow(field as TItem & { id: string }, index, () => { remove(index); }),
      )}
      <button
        type="button"
        onClick={() => { append(newItem() as never); }}
      >
        {addLabel}
      </button>
    </div>
  );
}
