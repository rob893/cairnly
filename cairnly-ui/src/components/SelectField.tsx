import type { Key } from 'react';
import { Label, ListBox, Select } from '@heroui/react';

/** A single option in a {@link SelectField}. */
export interface SelectOption<T extends string> {
  value: T;
  label: string;
}

interface SelectFieldProps<T extends string> {
  /** Optional visible label rendered above the trigger. */
  label?: string;
  /** Accessible label used when no visible `label` is provided. */
  'aria-label'?: string;
  /** The currently selected value. */
  value: T;
  /** Called with the newly selected value. */
  onChange(value: T): void;
  /** The selectable options. */
  options: ReadonlyArray<SelectOption<T>>;
  isDisabled?: boolean;
  className?: string;
}

/**
 * A thin wrapper over HeroUI's compound {@link Select} that renders a labeled
 * single-select dropdown from a list of `{ value, label }` options.
 *
 * @typeParam T The string union of allowed option values.
 */
export function SelectField<T extends string>({
  label,
  value,
  onChange,
  options,
  isDisabled,
  className,
  'aria-label': ariaLabel
}: SelectFieldProps<T>) {
  return (
    <Select
      aria-label={ariaLabel ?? label}
      selectedKey={value}
      onSelectionChange={(key: Key | null) => {
        if (key !== null) {
          onChange(key as T);
        }
      }}
      isDisabled={isDisabled}
      className={className ?? 'w-full'}
    >
      {label && <Label className="mb-1 block text-sm">{label}</Label>}
      <Select.Trigger className="w-full">
        <Select.Value />
        <Select.Indicator />
      </Select.Trigger>
      <Select.Popover className="min-w-[var(--trigger-width)]">
        <ListBox>
          {options.map(option => (
            <ListBox.Item key={option.value} id={option.value}>
              {option.label}
            </ListBox.Item>
          ))}
        </ListBox>
      </Select.Popover>
    </Select>
  );
}
