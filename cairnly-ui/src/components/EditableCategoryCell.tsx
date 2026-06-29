import { useState } from 'react';
import { CategorySelect } from './CategorySelect';
import type { Category, CategoryKind } from '../types/categories';

/** Props for a reusable inline category picker used by editable tables. */
export interface EditableCategoryCellProps<TItem> {
  /** Item passed back to `onSave` when the category changes. */
  item: TItem;
  /** Current category identifier. */
  categoryId: number;
  /** Resolved category shown while the cell is not editing. */
  category: Category | undefined;
  /** Optional kind filter for category choices. */
  categoryKind?: CategoryKind;
  /** Accessible label for the read-only edit button. */
  editAriaLabel: string;
  /** CSS classes for the read-only edit button. */
  buttonClassName?: string;
  /** Called when the user selects a different category. */
  onSave(item: TItem, categoryId: number): Promise<void>;
}

/** Inline category cell that switches to an open HeroUI category picker on click. */
export function EditableCategoryCell<TItem>({
  item,
  categoryId,
  category,
  categoryKind,
  editAriaLabel,
  buttonClassName,
  onSave
}: EditableCategoryCellProps<TItem>) {
  const [editing, setEditing] = useState(false);

  if (!editing) {
    return (
      <button type="button" onClick={() => setEditing(true)} aria-label={editAriaLabel} className={buttonClassName}>
        {category?.icon && <span aria-hidden="true">{category.icon}</span>}
        <span className="truncate">{category?.name || '—'}</span>
      </button>
    );
  }

  return (
    <CategorySelect
      aria-label="Category"
      value={categoryId}
      kind={categoryKind}
      defaultOpen
      onChange={nextCategoryId => {
        if (nextCategoryId !== categoryId) {
          void onSave(item, nextCategoryId);
        }
        setEditing(false);
      }}
      onOpenChange={open => {
        if (!open) {
          setEditing(false);
        }
      }}
      className="w-full min-w-44"
    />
  );
}
