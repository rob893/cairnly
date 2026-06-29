import { useEffect, useState } from 'react';

/** Props for a reusable inline text editor used inside dense table/list rows. */
export interface EditableTextCellProps<TItem> {
  /** Item passed back to `onSave` when the edited value changes. */
  item: TItem;
  /** Current persisted value. */
  value: string;
  /** Read-only text shown before editing; defaults to `value`. */
  displayValue?: string;
  /** Accessible label for the read-only edit button. */
  editAriaLabel: string;
  /** Accessible label for the editing input. */
  inputAriaLabel: string;
  /** CSS classes for the read-only edit button. */
  buttonClassName?: string;
  /** CSS classes for the editing input. */
  inputClassName?: string;
  /** Called with the trimmed value when the user commits a changed, non-empty edit. */
  onSave(item: TItem, value: string): Promise<void>;
}

/** Inline text cell that saves on blur or Enter and reverts on Escape or failed saves. */
export function EditableTextCell<TItem>({
  item,
  value,
  displayValue,
  editAriaLabel,
  inputAriaLabel,
  buttonClassName,
  inputClassName,
  onSave
}: EditableTextCellProps<TItem>) {
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(value);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!editing) {
      setDraft(value);
    }
  }, [value, editing]);

  function cancel(): void {
    setDraft(value);
    setEditing(false);
  }

  async function commit(): Promise<void> {
    const trimmed = draft.trim();
    if (!trimmed || trimmed === value) {
      cancel();
      return;
    }

    setSaving(true);
    try {
      await onSave(item, trimmed);
      setEditing(false);
    } catch {
      setDraft(value);
    } finally {
      setSaving(false);
    }
  }

  if (!editing) {
    return (
      <button type="button" onClick={() => setEditing(true)} aria-label={editAriaLabel} className={buttonClassName}>
        {displayValue ?? value}
      </button>
    );
  }

  return (
    <input
      type="text"
      autoFocus
      value={draft}
      disabled={saving}
      aria-label={inputAriaLabel}
      onChange={event => setDraft(event.target.value)}
      onBlur={() => void commit()}
      onKeyDown={event => {
        if (event.key === 'Enter') {
          event.preventDefault();
          void commit();
        } else if (event.key === 'Escape') {
          event.preventDefault();
          cancel();
        }
      }}
      className={inputClassName}
    />
  );
}
