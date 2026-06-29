import { Dropdown } from '@heroui/react';
import { ChevronDown } from 'lucide-react';

/** A Monarch-style dropdown for switching a chart grouping mode. */
export function ChartModeDropdown<T extends string>({
  options,
  value,
  onChange,
  ariaLabel
}: {
  options: ReadonlyArray<{ id: T; label: string }>;
  value: T;
  onChange(value: T): void;
  ariaLabel: string;
}) {
  const activeLabel = options.find(option => option.id === value)?.label ?? options[0]?.label ?? '';

  return (
    <Dropdown>
      <Dropdown.Trigger
        aria-label={ariaLabel}
        className="inline-flex items-center gap-2 rounded-lg border border-border bg-surface-secondary px-3 py-1.5 text-sm font-medium text-foreground outline-none transition-colors hover:bg-surface focus-visible:ring-2 focus-visible:ring-focus"
      >
        {activeLabel}
        <ChevronDown className="size-4 text-muted" />
      </Dropdown.Trigger>
      <Dropdown.Popover placement="bottom end" className="min-w-48">
        <Dropdown.Menu
          aria-label={ariaLabel}
          selectionMode="single"
          selectedKeys={new Set([value])}
          onAction={key => onChange(key as T)}
        >
          {options.map(option => (
            <Dropdown.Item key={option.id} id={option.id}>
              {option.label}
            </Dropdown.Item>
          ))}
        </Dropdown.Menu>
      </Dropdown.Popover>
    </Dropdown>
  );
}
