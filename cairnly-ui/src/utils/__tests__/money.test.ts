import { describe, it, expect } from 'vitest';
import { currencyFractionDigits, minorToMajor, majorToMinor, formatMoney, parseMoneyToMinor } from '../money';
import { cadenceBreakdown, periodsPerYear } from '../cadence';

describe('currencyFractionDigits', () => {
  it('returns 2 for USD', () => {
    expect(currencyFractionDigits('USD')).toBe(2);
  });

  it('returns 0 for JPY', () => {
    expect(currencyFractionDigits('JPY')).toBe(0);
  });

  it('is case-insensitive', () => {
    expect(currencyFractionDigits('usd')).toBe(2);
  });

  it('falls back to 2 for unknown codes', () => {
    expect(currencyFractionDigits('ZZZ')).toBe(2);
  });
});

describe('minorToMajor / majorToMinor', () => {
  it('converts USD minor units to major units', () => {
    expect(minorToMajor(1234, 'USD')).toBeCloseTo(12.34);
  });

  it('converts JPY minor units to major units (no decimals)', () => {
    expect(minorToMajor(1234, 'JPY')).toBe(1234);
  });

  it('converts major units back to minor units, rounding', () => {
    expect(majorToMinor(12.34, 'USD')).toBe(1234);
    expect(majorToMinor(12.345, 'USD')).toBe(1235);
  });

  it('round-trips cleanly', () => {
    expect(minorToMajor(majorToMinor(99.99, 'USD'), 'USD')).toBeCloseTo(99.99);
  });
});

describe('formatMoney', () => {
  it('formats USD minor units as a currency string', () => {
    expect(formatMoney(123456, 'USD')).toBe('$1,234.56');
  });

  it('formats zero', () => {
    expect(formatMoney(0, 'USD')).toBe('$0.00');
  });

  it('falls back gracefully for unknown currencies', () => {
    const result = formatMoney(1234, 'ZZZ');
    expect(result).toContain('12.34');
  });
});

describe('parseMoneyToMinor', () => {
  it('parses a plain decimal', () => {
    expect(parseMoneyToMinor('12.34', 'USD')).toBe(1234);
  });

  it('tolerates currency symbols and grouping', () => {
    expect(parseMoneyToMinor('$1,234.56', 'USD')).toBe(123456);
  });

  it('returns null for empty or invalid input', () => {
    expect(parseMoneyToMinor('', 'USD')).toBeNull();
    expect(parseMoneyToMinor('abc', 'USD')).toBeNull();
    expect(parseMoneyToMinor('.', 'USD')).toBeNull();
  });

  it('respects currencies without minor units', () => {
    expect(parseMoneyToMinor('1500', 'JPY')).toBe(1500);
  });
});

describe('periodsPerYear', () => {
  it('matches the API cadence periods', () => {
    expect(periodsPerYear('Daily')).toBe(365);
    expect(periodsPerYear('Weekly')).toBe(52);
    expect(periodsPerYear('BiWeekly')).toBe(26);
    expect(periodsPerYear('SemiMonthly')).toBe(24);
    expect(periodsPerYear('Monthly')).toBe(12);
    expect(periodsPerYear('Quarterly')).toBe(4);
    expect(periodsPerYear('Annual')).toBe(1);
  });
});

describe('cadenceBreakdown', () => {
  it('normalizes a weekly $500 expense across cadences (matches screenshot)', () => {
    // $500.00 weekly => 50000 minor units per week.
    const breakdown = cadenceBreakdown(50000, 'Weekly');

    expect(breakdown.annual).toBe(50000 * 52); // $26,000.00
    expect(formatMoney(breakdown.annual, 'USD')).toBe('$26,000.00');
    expect(formatMoney(breakdown.weekly, 'USD')).toBe('$500.00');
    expect(formatMoney(breakdown.monthly, 'USD')).toBe('$2,166.67');
    expect(formatMoney(breakdown.daily, 'USD')).toBe('$71.23');
  });

  it('normalizes a monthly expense', () => {
    // $5.00 monthly => 500 minor units per month => $60/yr.
    const breakdown = cadenceBreakdown(500, 'Monthly');

    expect(breakdown.annual).toBe(6000);
    expect(formatMoney(breakdown.annual, 'USD')).toBe('$60.00');
    expect(formatMoney(breakdown.monthly, 'USD')).toBe('$5.00');
  });
});
