/** Bucket granularity for the cash-flow series. */
export type CashFlowPeriod = 'Monthly' | 'Quarterly' | 'Yearly';

/** Period options with human-readable labels for the page toggle. */
export const cashFlowPeriods: ReadonlyArray<{ value: CashFlowPeriod; label: string }> = [
  { value: 'Monthly', label: 'Monthly' },
  { value: 'Quarterly', label: 'Quarterly' },
  { value: 'Yearly', label: 'Yearly' }
];

/** One breakdown row (category, group, or merchant) with its share of the section total. */
export interface CashFlowBreakdownItem {
  /** Drill key: category ID (category/group) or merchant name; empty for the "Other" row. */
  key: string;
  label: string;
  icon?: string | null;
  /** Absolute total in minor units. */
  amount: number;
  /** Share of the section total (0-100). */
  percent: number;
}

/** Cash-flow breakdowns for one section, grouped three ways. */
export interface CashFlowBreakdowns {
  byCategory: CashFlowBreakdownItem[];
  byGroup: CashFlowBreakdownItem[];
  byMerchant: CashFlowBreakdownItem[];
}

/** One period bucket: income/expense/net plus its own income & expense breakdowns. */
export interface CashFlowPoint {
  /** First day of the bucket, ISO `YYYY-MM-DD`. */
  periodStart: string;
  label: string;
  income: number;
  expenses: number;
  net: number;
  incomeBreakdowns: CashFlowBreakdowns;
  expenseBreakdowns: CashFlowBreakdowns;
}

/** Headline totals across the window. */
export interface CashFlowSummary {
  income: number;
  expenses: number;
  savings: number;
  savingsRate: number;
}

/** The full cash-flow report. */
export interface CashFlowReport {
  currency: string;
  period: CashFlowPeriod;
  summary: CashFlowSummary;
  series: CashFlowPoint[];
}

/** The dimension a breakdown is grouped by. */
export type CashFlowDimension = 'category' | 'group' | 'merchant';
