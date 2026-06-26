import type { SpendingPlanCadence, CadenceAmounts } from '../types/spendingPlans';

/** Number of days, weeks, and months per year used to normalize amounts. */
const DAYS_PER_YEAR = 365;
const WEEKS_PER_YEAR = 52;
const MONTHS_PER_YEAR = 12;

/**
 * The number of times a cadence occurs per year, mirroring the API's
 * `SpendingPlanCadenceExtensions.PeriodsPerYear` so client-side cost breakdowns match
 * the server's normalized summary.
 */
export function periodsPerYear(cadence: SpendingPlanCadence): number {
  switch (cadence) {
    case 'Daily':
      return DAYS_PER_YEAR;
    case 'Weekly':
      return WEEKS_PER_YEAR;
    case 'BiWeekly':
      return 26;
    case 'SemiMonthly':
      return 24;
    case 'Monthly':
      return MONTHS_PER_YEAR;
    case 'Quarterly':
      return 4;
    case 'Annual':
      return 1;
    default:
      return 1;
  }
}

/**
 * Normalizes a single line item's per-period amount into daily / weekly / monthly
 * / annual costs.
 *
 * The annual cost is `amount * periodsPerYear(cadence)`; the other columns divide
 * that annual figure by the number of days/weeks/months in a year. Amounts stay
 * in minor units and may be fractional — formatting rounds for display, matching
 * how the spendingPlan summary is presented.
 *
 * @param amount The per-period amount in integer minor units.
 * @param cadence The line item's recurrence cadence.
 * @returns The cost normalized across cadence columns, in (possibly fractional) minor units.
 */
export function cadenceBreakdown(amount: number, cadence: SpendingPlanCadence): CadenceAmounts {
  const annual = amount * periodsPerYear(cadence);

  return {
    daily: annual / DAYS_PER_YEAR,
    weekly: annual / WEEKS_PER_YEAR,
    monthly: annual / MONTHS_PER_YEAR,
    annual
  };
}
