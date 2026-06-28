import type { Key } from 'react';
import { Header } from 'react-aria-components';
import { Label, ListBox, Select } from '@heroui/react';
import { useCategories } from '../hooks/categories';
import type { CategoryKind } from '../types/categories';

interface CategorySelectProps {
  /** Optional visible label rendered above the trigger. */
  label?: string;
  /** Accessible label used when no visible `label` is provided. */
  'aria-label'?: string;
  /** The selected category ID, or `null` when none is chosen. */
  value: number | null;
  /** Called with the newly selected category ID. */
  onChange(categoryId: number): void;
  /** Restricts selectable categories to this kind (e.g. only Income for income lines). */
  kind?: CategoryKind;
  isDisabled?: boolean;
  className?: string;
  /** Marks the field required (renders an asterisk on the label). */
  isRequired?: boolean;
  /** When true, the popover starts open (useful for inline edit cells). */
  defaultOpen?: boolean;
  /** Notified when the popover opens or closes. */
  onOpenChange?(isOpen: boolean): void;
}

/**
 * A grouped category picker. Only leaf categories (children of a group) are
 * selectable; group headers are shown as non-interactive section labels. When
 * `kind` is provided, only groups of that kind are listed.
 */
export function CategorySelect({
  label,
  value,
  onChange,
  kind,
  isDisabled,
  className,
  isRequired,
  defaultOpen,
  onOpenChange,
  'aria-label': ariaLabel
}: CategorySelectProps) {
  const { groups, isLoading } = useCategories();

  const visibleGroups = groups
    .filter(group => (kind ? group.parent.kind === kind : true))
    .filter(group => group.children.length > 0);

  return (
    <Select
      aria-label={ariaLabel ?? label}
      selectedKey={value}
      onSelectionChange={(key: Key | null) => {
        if (key !== null) {
          onChange(Number(key));
        }
      }}
      defaultOpen={defaultOpen}
      onOpenChange={onOpenChange}
      isDisabled={isDisabled || isLoading}
      isRequired={isRequired}
      className={className ?? 'w-full'}
    >
      {label && <Label className="mb-1 block text-sm">{label}</Label>}
      <Select.Trigger className="w-full">
        <Select.Value />
        <Select.Indicator />
      </Select.Trigger>
      <Select.Popover className="min-w-[var(--trigger-width)]">
        <ListBox>
          {visibleGroups.map(group => (
            <ListBox.Section key={group.parent.id}>
              <Header className="px-2 py-1 text-xs font-semibold uppercase tracking-wide text-muted">
                {group.parent.name}
              </Header>
              {group.children.map(child => (
                <ListBox.Item key={child.id} id={child.id} textValue={child.name}>
                  <span className="flex items-center gap-2">
                    {child.icon && <span aria-hidden="true">{child.icon}</span>}
                    {child.name}
                  </span>
                </ListBox.Item>
              ))}
            </ListBox.Section>
          ))}
        </ListBox>
      </Select.Popover>
    </Select>
  );
}
