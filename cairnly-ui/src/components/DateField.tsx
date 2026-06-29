import { Calendar, DatePicker, Label } from '@heroui/react';
import { DateInput, DateSegment, Dialog, Group } from 'react-aria-components';
import { parseDate, type CalendarDate, type DateValue } from '@internationalized/date';

interface DateFieldProps {
  /** Optional visible label rendered above the field. */
  label?: string;
  /** Accessible label used when no visible `label` is provided. */
  'aria-label'?: string;
  /** The selected date as an ISO `YYYY-MM-DD` string, or empty when unset. */
  value: string;
  /** Called with the newly selected date as an ISO `YYYY-MM-DD` string. */
  onChange(value: string): void;
  isRequired?: boolean;
  isDisabled?: boolean;
  className?: string;
}

/** Parses an ISO `YYYY-MM-DD` string into a CalendarDate, or `null` when invalid/empty. */
function toDateValue(value: string): CalendarDate | null {
  if (!value) {
    return null;
  }

  try {
    return parseDate(value);
  } catch {
    return null;
  }
}

/**
 * A labeled date picker built on HeroUI's compound {@link DatePicker} and
 * {@link Calendar}. Accepts and emits ISO `YYYY-MM-DD` strings so callers can work
 * with plain dates rather than `@internationalized/date` values.
 */
export function DateField({
  label,
  value,
  onChange,
  isRequired,
  isDisabled,
  className,
  'aria-label': ariaLabel
}: DateFieldProps) {
  return (
    <DatePicker
      aria-label={ariaLabel ?? label}
      value={toDateValue(value)}
      onChange={(next: DateValue | null) => onChange(next ? next.toString() : '')}
      isRequired={isRequired}
      isDisabled={isDisabled}
      className={className ?? 'w-full'}
    >
      {label && <Label isRequired={isRequired}>{label}</Label>}
      <Group className="mt-1 flex w-full items-center gap-2 rounded-[var(--field-radius)] border border-[var(--field-border)] bg-[var(--field-background)] px-3 py-2 text-sm text-[var(--field-foreground)] outline-none focus-within:ring-2 focus-within:ring-focus data-[disabled]:opacity-50">
        <DateInput className="flex shrink-0 items-center">
          {segment => (
            <DateSegment
              segment={segment}
              className="rounded px-0.5 tabular-nums outline-none data-[placeholder]:text-muted data-[focused]:bg-accent data-[focused]:text-accent-foreground"
            />
          )}
        </DateInput>
        <DatePicker.Trigger className="flex justify-end text-muted outline-none transition-colors hover:text-foreground focus-visible:text-foreground">
          <DatePicker.TriggerIndicator />
        </DatePicker.Trigger>
      </Group>
      <DatePicker.Popover>
        <Dialog className="p-3 outline-none">
          <Calendar>
            <Calendar.Header>
              <Calendar.NavButton slot="previous" />
              <Calendar.Heading />
              <Calendar.NavButton slot="next" />
            </Calendar.Header>
            <Calendar.Grid>
              <Calendar.GridHeader>{day => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}</Calendar.GridHeader>
              <Calendar.GridBody>{date => <Calendar.Cell date={date} />}</Calendar.GridBody>
            </Calendar.Grid>
          </Calendar>
        </Dialog>
      </DatePicker.Popover>
    </DatePicker>
  );
}
