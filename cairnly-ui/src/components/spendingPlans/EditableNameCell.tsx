import { EditableTextCell } from '../EditableTextCell';
import type { LineItem } from './lineItemTypes';

interface EditableNameCellProps {
  item: LineItem;
  onSave(item: LineItem, patch: { name: string }): Promise<void>;
}

/** Spending-plan line item name cell backed by the shared inline text editor. */
export function EditableNameCell({ item, onSave }: EditableNameCellProps) {
  return (
    <EditableTextCell
      item={item}
      value={item.name}
      editAriaLabel={`Edit ${item.name} name`}
      inputAriaLabel="Name"
      buttonClassName="-mx-1 max-w-full truncate rounded px-1 text-left font-medium transition-colors hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
      inputClassName="w-full min-w-32 rounded border border-border bg-surface px-2 py-1 text-sm font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus"
      onSave={(lineItem, name) => onSave(lineItem, { name })}
    />
  );
}
