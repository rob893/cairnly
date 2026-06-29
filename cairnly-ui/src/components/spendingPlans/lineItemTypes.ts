import type { CadenceAmounts, SpendingPlanExpense, SpendingPlanIncome } from '../../types/spendingPlans';

/** Spending plan income or expense item rendered by the shared line-item table. */
export type LineItem = SpendingPlanIncome | SpendingPlanExpense;

/** Inline edit patch supported by the line-item table cells. */
export interface LineItemInlinePatch {
  name?: string;
  categoryId?: number;
}

/** Amount columns displayed for expense cadence breakdowns. */
export const COST_COLUMNS: ReadonlyArray<{ key: keyof CadenceAmounts; label: string }> = [
  { key: 'daily', label: 'Daily Cost' },
  { key: 'weekly', label: 'Weekly Cost' },
  { key: 'monthly', label: 'Monthly Cost' },
  { key: 'annual', label: 'Annual Cost' }
];
