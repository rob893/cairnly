import type { CadenceAmounts } from '../types/budgets';
import type { SelectOption } from '../components/SelectField';

/** The normalized cadence keys the dashboard can display. */
export type DashboardCadence = keyof CadenceAmounts;

/** Cadence options for the dashboard cadence selector. */
export const dashboardCadenceOptions: ReadonlyArray<SelectOption<DashboardCadence>> = [
  { value: 'daily', label: 'Daily' },
  { value: 'weekly', label: 'Weekly' },
  { value: 'monthly', label: 'Monthly' },
  { value: 'annual', label: 'Annual' }
];

/** Human-readable noun for a cadence period (used in labels like "this monthly"). */
export const dashboardCadenceLabel: Record<DashboardCadence, string> = {
  daily: 'Daily',
  weekly: 'Weekly',
  monthly: 'Monthly',
  annual: 'Annual'
};

/**
 * Computes the fraction of planned income consumed by expenses, clamped to
 * `[0, 1]` for progress-bar display.
 *
 * @param income Planned income in minor units.
 * @param expenses Expenses in minor units.
 * @returns A ratio in `[0, 1]`; `0` when there is no income.
 */
export function spentRatio(income: number, expenses: number): number {
  if (income <= 0) {
    return expenses > 0 ? 1 : 0;
  }

  return Math.min(Math.max(expenses / income, 0), 1);
}

/** Progress-bar color tone derived from how much of income is spent / overspent. */
export type SpendTone = 'under' | 'warning' | 'over';

/**
 * Classifies spending against income: `over` when expenses exceed income,
 * `warning` within 10% of the limit, otherwise `under`.
 */
export function spendTone(income: number, expenses: number): SpendTone {
  if (expenses > income) {
    return 'over';
  }

  if (income > 0 && expenses / income >= 0.9) {
    return 'warning';
  }

  return 'under';
}

/** Maps a {@link SpendTone} to its progress-bar fill class. */
export const spendToneBarClass: Record<SpendTone, string> = {
  under: 'bg-success',
  warning: 'bg-warning',
  over: 'bg-danger'
};
