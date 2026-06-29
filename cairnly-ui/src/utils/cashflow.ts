import type {
  CashFlowBreakdowns,
  CashFlowDimension,
  CashFlowPeriod,
  CashFlowPoint,
  CashFlowSummary
} from '../types/reports';

/** Adds an ISO `YYYY-MM-DD` start to the inclusive last day of its period bucket. */
export function periodEndIso(startIso: string, period: CashFlowPeriod): string {
  const [y, m, d] = startIso.split('-').map(Number);
  const start = new Date(Date.UTC(y, m - 1, d));
  const end = new Date(start);

  if (period === 'Yearly') {
    end.setUTCFullYear(end.getUTCFullYear() + 1);
  } else if (period === 'Quarterly') {
    end.setUTCMonth(end.getUTCMonth() + 3);
  } else {
    end.setUTCMonth(end.getUTCMonth() + 1);
  }

  end.setUTCDate(end.getUTCDate() - 1);
  return end.toISOString().slice(0, 10);
}

/** The inclusive [from, to] date range covered by the series (empty when no data). */
export function seriesRange(series: CashFlowPoint[], period: CashFlowPeriod): { from: string; to: string } | null {
  if (series.length === 0) {
    return null;
  }

  return { from: series[0].periodStart, to: periodEndIso(series[series.length - 1].periodStart, period) };
}

/** Sums breakdown rows by key, recomputing each share against the new total; sorted desc. */
function mergeBreakdowns(parts: CashFlowBreakdowns[]): CashFlowBreakdowns {
  const merge = (lists: CashFlowBreakdowns['byCategory'][]) => {
    const byKey = new Map<string, { label: string; icon?: string | null; amount: number }>();
    for (const list of lists) {
      for (const item of list) {
        const existing = byKey.get(item.key);
        if (existing) {
          existing.amount += item.amount;
        } else {
          byKey.set(item.key, { label: item.label, icon: item.icon, amount: item.amount });
        }
      }
    }
    const total = [...byKey.values()].reduce((sum, v) => sum + v.amount, 0);
    return [...byKey.entries()]
      .map(([key, v]) => ({
        key,
        label: v.label,
        icon: v.icon,
        amount: v.amount,
        percent: total > 0 ? (v.amount / total) * 100 : 0
      }))
      .sort((a, b) => b.amount - a.amount);
  };

  return {
    byCategory: merge(parts.map(p => p.byCategory)),
    byGroup: merge(parts.map(p => p.byGroup)),
    byMerchant: merge(parts.map(p => p.byMerchant))
  };
}

/** Aggregates the whole series into one synthetic point so breakdowns mirror the window. */
export function aggregatePoint(series: CashFlowPoint[]): {
  income: number;
  expenses: number;
  incomeBreakdowns: CashFlowBreakdowns;
  expenseBreakdowns: CashFlowBreakdowns;
} {
  return {
    income: series.reduce((sum, p) => sum + p.income, 0),
    expenses: series.reduce((sum, p) => sum + p.expenses, 0),
    incomeBreakdowns: mergeBreakdowns(series.map(p => p.incomeBreakdowns)),
    expenseBreakdowns: mergeBreakdowns(series.map(p => p.expenseBreakdowns))
  };
}

/** Builds the headline summary for a single income/expense pair. */
export function summaryFor(income: number, expenses: number): CashFlowSummary {
  const savings = income - expenses;
  return { income, expenses, savings, savingsRate: income > 0 ? (savings / income) * 100 : 0 };
}

/** A single key's amount per period, for the detail-page chart. */
export interface CashFlowKeyPoint {
  periodStart: string;
  label: string;
  amount: number;
}

/**
 * Extracts one breakdown key's amount per period for the given section/dimension,
 * so the detail page can chart a single category/group/merchant over time.
 *
 * @param series The full per-period series.
 * @param section Whether the key is an income or expense breakdown.
 * @param dimension Which breakdown grouping the key belongs to.
 * @param key The category id (category/group) or merchant name.
 */
export function keyAmountByPeriod(
  series: CashFlowPoint[],
  section: 'income' | 'expense',
  dimension: CashFlowDimension,
  key: string
): CashFlowKeyPoint[] {
  const list = dimension === 'group' ? 'byGroup' : dimension === 'merchant' ? 'byMerchant' : 'byCategory';
  return series.map(point => {
    const breakdowns = section === 'income' ? point.incomeBreakdowns : point.expenseBreakdowns;
    const amount = breakdowns[list].find(item => item.key === key)?.amount ?? 0;
    return { periodStart: point.periodStart, label: point.label, amount };
  });
}
