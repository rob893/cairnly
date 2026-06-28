/** Helpers for formatting dates and timestamps for display. */

const relativeTimeFormatter = new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' });

const SHORT_DATE_FORMATTER = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' });

const LONG_DATE_FORMATTER = new Intl.DateTimeFormat(undefined, { month: 'long', day: 'numeric', year: 'numeric' });

const DIVISIONS: ReadonlyArray<{ amount: number; unit: Intl.RelativeTimeFormatUnit }> = [
  { amount: 60, unit: 'second' },
  { amount: 60, unit: 'minute' },
  { amount: 24, unit: 'hour' },
  { amount: 7, unit: 'day' },
  { amount: 4.34524, unit: 'week' },
  { amount: 12, unit: 'month' },
  { amount: Number.POSITIVE_INFINITY, unit: 'year' }
];

/**
 * Formats an ISO timestamp as a short relative label (e.g. "2 hours ago",
 * "yesterday"). Returns an empty string for missing or invalid input.
 *
 * @param iso The ISO 8601 timestamp.
 */
export function formatRelativeTime(iso: string | undefined | null): string {
  if (!iso) {
    return '';
  }

  const date = new Date(iso);

  if (Number.isNaN(date.getTime())) {
    return '';
  }

  let duration = (date.getTime() - Date.now()) / 1000;

  for (const division of DIVISIONS) {
    if (Math.abs(duration) < division.amount) {
      return relativeTimeFormatter.format(Math.round(duration), division.unit);
    }

    duration /= division.amount;
  }

  return '';
}

/**
 * Formats a date (ISO date or date-time string) as a compact "Jun 22" label.
 *
 * @param iso The ISO date or date-time string.
 */
export function formatShortDate(iso: string): string {
  const date = new Date(iso);

  if (Number.isNaN(date.getTime())) {
    return iso;
  }

  return SHORT_DATE_FORMATTER.format(date);
}

/**
 * Formats a date (ISO date or date-time string) as a full "June 22, 2026" label.
 *
 * @param iso The ISO date or date-time string.
 */
export function formatLongDate(iso: string): string {
  const date = new Date(iso);

  if (Number.isNaN(date.getTime())) {
    return iso;
  }

  return LONG_DATE_FORMATTER.format(date);
}
