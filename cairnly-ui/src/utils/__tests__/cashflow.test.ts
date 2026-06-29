import { describe, it, expect } from 'vitest';
import { aggregatePoint, keyAmountByPeriod, periodEndIso, seriesRange, summaryFor } from '../cashflow';
import type { CashFlowBreakdowns, CashFlowPoint } from '../../types/reports';

function emptyBreakdowns(): CashFlowBreakdowns {
  return { byCategory: [], byGroup: [], byMerchant: [] };
}

function point(
  start: string,
  income: number,
  expenses: number,
  expenseCats: CashFlowBreakdowns['byCategory']
): CashFlowPoint {
  return {
    periodStart: start,
    label: start,
    income,
    expenses,
    net: income - expenses,
    incomeBreakdowns: emptyBreakdowns(),
    expenseBreakdowns: { byCategory: expenseCats, byGroup: [], byMerchant: [] }
  };
}

describe('periodEndIso', () => {
  it('returns the last day of a month', () => {
    expect(periodEndIso('2026-05-01', 'Monthly')).toBe('2026-05-31');
  });

  it('returns the last day of a quarter', () => {
    expect(periodEndIso('2026-04-01', 'Quarterly')).toBe('2026-06-30');
  });

  it('returns the last day of a year', () => {
    expect(periodEndIso('2026-01-01', 'Yearly')).toBe('2026-12-31');
  });
});

describe('seriesRange', () => {
  it('spans the first start to the last period end', () => {
    const range = seriesRange([point('2026-01-01', 0, 0, []), point('2026-02-01', 0, 0, [])], 'Monthly');
    expect(range).toEqual({ from: '2026-01-01', to: '2026-02-28' });
  });

  it('is null for an empty series', () => {
    expect(seriesRange([], 'Monthly')).toBeNull();
  });
});

describe('aggregatePoint', () => {
  it('sums amounts and merges breakdown rows by key with recomputed shares', () => {
    const cat = (key: string, amount: number) => ({ key, label: `c${key}`, amount, percent: 0 });
    const result = aggregatePoint([
      point('2026-01-01', 1000, 400, [cat('1', 300), cat('2', 100)]),
      point('2026-02-01', 500, 200, [cat('1', 200)])
    ]);

    expect(result.income).toBe(1500);
    expect(result.expenses).toBe(600);
    const top = result.expenseBreakdowns.byCategory[0];
    expect(top.key).toBe('1');
    expect(top.amount).toBe(500);
    expect(top.percent).toBeCloseTo((500 / 600) * 100, 3);
  });
});

describe('summaryFor', () => {
  it('computes savings and rate', () => {
    expect(summaryFor(1000, 600)).toEqual({ income: 1000, expenses: 600, savings: 400, savingsRate: 40 });
  });

  it('is zero rate with no income', () => {
    expect(summaryFor(0, 250).savingsRate).toBe(0);
  });
});

describe('keyAmountByPeriod', () => {
  it('extracts one expense category amount per period (0 when absent)', () => {
    const cat = (key: string, amount: number) => ({ key, label: `c${key}`, amount, percent: 0 });
    const series = [
      point('2026-01-01', 0, 400, [cat('29', 300), cat('37', 100)]),
      point('2026-02-01', 0, 200, [cat('37', 200)])
    ];
    expect(keyAmountByPeriod(series, 'expense', 'category', '29')).toEqual([
      { periodStart: '2026-01-01', label: '2026-01-01', amount: 300 },
      { periodStart: '2026-02-01', label: '2026-02-01', amount: 0 }
    ]);
  });
});
