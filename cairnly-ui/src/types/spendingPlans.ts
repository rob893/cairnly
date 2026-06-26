import type { CursorPaginationQueryParameters } from './pagination';

/**
 * How often a spendingPlan income or expense line item recurs. Serialized as a string
 * by the API (`JsonStringEnumConverter`).
 */
export type SpendingPlanCadence = 'Daily' | 'Weekly' | 'BiWeekly' | 'SemiMonthly' | 'Monthly' | 'Quarterly' | 'Annual';

/** Ordered list of cadences with human-readable labels for selects/tables. */
export const spendingPlanCadences: ReadonlyArray<{ value: SpendingPlanCadence; label: string }> = [
  { value: 'Daily', label: 'Daily' },
  { value: 'Weekly', label: 'Weekly' },
  { value: 'BiWeekly', label: 'Bi-weekly' },
  { value: 'SemiMonthly', label: 'Semi-monthly' },
  { value: 'Monthly', label: 'Monthly' },
  { value: 'Quarterly', label: 'Quarterly' },
  { value: 'Annual', label: 'Annual' }
];

/** Looks up the display label for a cadence value. */
export function cadenceLabel(cadence: SpendingPlanCadence): string {
  return spendingPlanCadences.find(c => c.value === cadence)?.label ?? cadence;
}

/** A spendingPlan owned by the current user. */
export interface SpendingPlan {
  id: number;
  userId: number;
  name: string;
  description?: string | null;
  currency: string;
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

/** A single income line item belonging to a spendingPlan. */
export interface SpendingPlanIncome {
  id: number;
  userId: number;
  spendingPlanId: number;
  name: string;
  description?: string | null;
  amount: number;
  cadence: SpendingPlanCadence;
  categoryId: number;
  tagIds: number[];
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

/** A single expense line item belonging to a spendingPlan. */
export interface SpendingPlanExpense {
  id: number;
  userId: number;
  spendingPlanId: number;
  name: string;
  description?: string | null;
  amount: number;
  cadence: SpendingPlanCadence;
  categoryId: number;
  tagIds: number[];
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

/** A money amount (minor units) normalized across cadence columns. */
export interface CadenceAmounts {
  daily: number;
  weekly: number;
  monthly: number;
  annual: number;
}

/** Computed summary of a spendingPlan's normalized income, expenses, and remaining. */
export interface SpendingPlanSummary {
  spendingPlanId: number;
  currency: string;
  income: CadenceAmounts;
  expenses: CadenceAmounts;
  remaining: CadenceAmounts;
}

// Request models

/** Payload to create a new spendingPlan. */
export interface CreateSpendingPlanRequest {
  name: string;
  description?: string | null;
  currency: string;
  metadata?: Record<string, unknown> | null;
}

/** Payload to fully update an existing spendingPlan. */
export interface UpdateSpendingPlanRequest {
  name: string;
  description?: string | null;
  currency: string;
  metadata?: Record<string, unknown> | null;
}

/** Payload to create a new income line item. */
export interface CreateSpendingPlanIncomeRequest {
  name: string;
  description?: string | null;
  amount: number;
  cadence: SpendingPlanCadence;
  categoryId: number;
  tagIds?: number[] | null;
  metadata?: Record<string, unknown> | null;
}

/** Payload to fully update an existing income line item. */
export type UpdateSpendingPlanIncomeRequest = CreateSpendingPlanIncomeRequest;

/** Payload to create a new expense line item. */
export interface CreateSpendingPlanExpenseRequest {
  name: string;
  description?: string | null;
  amount: number;
  cadence: SpendingPlanCadence;
  categoryId: number;
  tagIds?: number[] | null;
  metadata?: Record<string, unknown> | null;
}

/** Payload to fully update an existing expense line item. */
export type UpdateSpendingPlanExpenseRequest = CreateSpendingPlanExpenseRequest;

// Query parameters

/** Query parameters for listing spendingPlans. */
export interface SpendingPlanQueryParameters extends CursorPaginationQueryParameters {
  name?: string;
}

/** Query parameters for listing a spendingPlan's income line items. */
export interface SpendingPlanIncomeQueryParameters extends CursorPaginationQueryParameters {
  name?: string;
}

/** Query parameters for listing a spendingPlan's expense line items. */
export interface SpendingPlanExpenseQueryParameters extends CursorPaginationQueryParameters {
  name?: string;
  cadence?: SpendingPlanCadence;
}
