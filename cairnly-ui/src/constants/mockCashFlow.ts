/**
 * Placeholder data for the Cash Flow page. No cash-flow API exists yet. Chart
 * series values are in major dollars (expenses negative so bars drop below zero);
 * summary and category amounts are integer **minor units** (cents).
 */

/** One month of income / expenses / net cash flow (major dollars). */
export interface MockCashFlowPoint {
  month: string;
  /** Income for the month (positive). */
  income: number;
  /** Expenses for the month (negative, so the bar drops below the axis). */
  expenses: number;
  /** Net cash flow line value. */
  net: number;
}

/** A category breakdown row for the income/expense lists. */
export interface MockCashFlowCategory {
  label: string;
  emoji: string;
  /** Amount in minor units. */
  amount: number;
  /** Share of the section total, as a percentage. */
  percent: number;
}

/** The placeholder currency for cash-flow figures. */
export const mockCashFlowCurrency = 'USD';

/** Monthly cash-flow series for the chart. */
export const mockCashFlowSeries: MockCashFlowPoint[] = [
  { month: 'Jul', income: 5200, expenses: -5600, net: -400 },
  { month: 'Aug', income: 5400, expenses: -5500, net: -100 },
  { month: 'Sep', income: 5300, expenses: -5200, net: 100 },
  { month: 'Oct', income: 5600, expenses: -5900, net: -300 },
  { month: 'Nov', income: 6100, expenses: -5400, net: 700 },
  { month: 'Dec', income: 8200, expenses: -6800, net: 1400 },
  { month: 'Jan', income: 6400, expenses: -7200, net: -800 },
  { month: 'Feb', income: 6000, expenses: -12500, net: -6500 },
  { month: 'Mar', income: 7000, expenses: -6100, net: 900 },
  { month: 'Apr', income: 9500, expenses: -6400, net: 3100 },
  { month: 'May', income: 6800, expenses: -5600, net: 1200 },
  { month: 'Jun', income: 6500, expenses: -5200, net: 1300 },
  { month: 'Jul ', income: 7200, expenses: -6000, net: 1200 },
  { month: 'Aug ', income: 7600, expenses: -5800, net: 1800 },
  { month: 'Sep ', income: 24000, expenses: -7400, net: 16600 },
  { month: 'Oct ', income: 8200, expenses: -6100, net: 2100 },
  { month: 'Nov ', income: 7100, expenses: -6300, net: 800 },
  { month: 'Dec ', income: 7400, expenses: -6000, net: 1400 },
  { month: 'Jan ', income: 7800, expenses: -6200, net: 1600 },
  { month: 'Feb ', income: 7300, expenses: -6100, net: 1200 },
  { month: 'Mar ', income: 7900, expenses: -6400, net: 1500 },
  { month: 'Apr ', income: 7600, expenses: -6300, net: 1300 },
  { month: 'May ', income: 7700, expenses: -6500, net: 1200 },
  { month: 'Jun ', income: 7121, expenses: -8954, net: -1833 }
];

/** Headline cash-flow summary for the selected period. */
export const mockCashFlowSummary = {
  income: 712100,
  expenses: 895400,
  savings: -183200,
  savingsRate: 0
};

/** Income breakdown by category. */
export const mockIncomeCategories: MockCashFlowCategory[] = [
  { label: 'Paychecks', emoji: '💵', amount: 592795, percent: 83.2 },
  { label: 'Dividends & Capital Gains', emoji: '🟦', amount: 70033, percent: 9.8 },
  { label: 'Other Income', emoji: '🪙', amount: 32456, percent: 4.6 },
  { label: 'Interest', emoji: '🌱', amount: 16841, percent: 2.4 }
];

/** Expense breakdown by category. */
export const mockExpenseCategories: MockCashFlowCategory[] = [
  { label: 'Mortgage', emoji: '🏡', amount: 445437, percent: 49.7 },
  { label: 'Home Improvement', emoji: '🔨', amount: 233410, percent: 26.1 },
  { label: 'Shopping', emoji: '🛍️', amount: 75456, percent: 8.4 },
  { label: 'Insurance', emoji: '🟣', amount: 28944, percent: 3.2 },
  { label: 'Groceries', emoji: '🍏', amount: 26878, percent: 3.0 },
  { label: 'Restaurants & Bars', emoji: '🍽️', amount: 22148, percent: 2.5 },
  { label: 'Rent', emoji: '🔑', amount: 15000, percent: 1.7 }
];
