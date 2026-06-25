/**
 * Helpers for working with money amounts expressed as integer **minor units**
 * (e.g. cents) in a given ISO 4217 currency, matching the API contract where all
 * budget amounts are stored and transmitted as `long` minor units.
 */

/** Cache of minor-unit fraction digits per currency to avoid rebuilding formatters. */
const fractionDigitsCache = new Map<string, number>();

/**
 * Resolves the number of minor-unit fraction digits for a currency (2 for USD,
 * 0 for JPY, 3 for BHD, etc.). Falls back to 2 for unknown codes.
 *
 * @param currency The ISO 4217 currency code.
 * @returns The number of fraction digits used by the currency.
 */
export function currencyFractionDigits(currency: string): number {
  const key = currency.toUpperCase();
  const cached = fractionDigitsCache.get(key);

  if (cached !== undefined) {
    return cached;
  }

  let digits: number;

  try {
    digits =
      new Intl.NumberFormat(undefined, { style: 'currency', currency: key }).resolvedOptions()
        .maximumFractionDigits ?? 2;
  } catch {
    digits = 2;
  }

  fractionDigitsCache.set(key, digits);
  return digits;
}

/**
 * Converts an integer minor-unit amount into a major-unit number (e.g. 1234 → 12.34
 * for USD).
 *
 * @param minorUnits The amount in integer minor units.
 * @param currency The ISO 4217 currency code.
 * @returns The amount in major units as a floating point number.
 */
export function minorToMajor(minorUnits: number, currency: string): number {
  return minorUnits / 10 ** currencyFractionDigits(currency);
}

/**
 * Converts a major-unit number into integer minor units (e.g. 12.34 → 1234 for USD).
 * The result is rounded to the nearest minor unit to avoid floating point drift.
 *
 * @param majorUnits The amount in major units.
 * @param currency The ISO 4217 currency code.
 * @returns The amount in integer minor units.
 */
export function majorToMinor(majorUnits: number, currency: string): number {
  return Math.round(majorUnits * 10 ** currencyFractionDigits(currency));
}

/**
 * Formats an integer minor-unit amount as a localized currency string.
 *
 * @param minorUnits The amount in integer minor units.
 * @param currency The ISO 4217 currency code.
 * @returns A localized currency string (e.g. `$1,234.56`). Falls back to a plain
 * number with the code appended for unknown currencies.
 */
export function formatMoney(minorUnits: number, currency: string): string {
  const major = minorToMajor(minorUnits, currency);

  try {
    return new Intl.NumberFormat(undefined, { style: 'currency', currency: currency.toUpperCase() }).format(major);
  } catch {
    return `${major.toFixed(2)} ${currency.toUpperCase()}`;
  }
}

/**
 * Parses a user-entered major-unit string into integer minor units.
 *
 * @param value The raw input string (currency symbols, spaces, and grouping
 * commas are tolerated).
 * @param currency The ISO 4217 currency code.
 * @returns The amount in integer minor units, or `null` when the input is not a
 * valid number.
 */
export function parseMoneyToMinor(value: string, currency: string): number | null {
  const cleaned = value.replace(/[^0-9.-]/g, '');

  if (cleaned === '' || cleaned === '-' || cleaned === '.') {
    return null;
  }

  const major = Number(cleaned);

  if (!Number.isFinite(major)) {
    return null;
  }

  return majorToMinor(major, currency);
}
