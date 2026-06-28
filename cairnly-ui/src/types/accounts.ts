import type { CursorPaginationQueryParameters } from './pagination';

/**
 * The kind of financial account. Serialized as a string by the API
 * (`JsonStringEnumConverter`).
 */
export type AccountType =
  | 'Checking'
  | 'Savings'
  | 'Brokerage'
  | 'Retirement'
  | 'Hsa'
  | 'Cash'
  | 'RealEstate'
  | 'Vehicle'
  | 'Crypto'
  | 'Mortgage'
  | 'CreditCard'
  | 'Loan'
  | 'Other';

/** Whether an account contributes to net worth as an asset or a liability. */
export type AccountClass = 'Asset' | 'Liability';

/** Human-readable labels for each account type. */
export const accountTypeLabel: Record<AccountType, string> = {
  Checking: 'Checking',
  Savings: 'Savings',
  Brokerage: 'Brokerage',
  Retirement: 'Retirement',
  Hsa: 'HSA',
  Cash: 'Cash',
  RealEstate: 'Real Estate',
  Vehicle: 'Vehicle',
  Crypto: 'Crypto',
  Mortgage: 'Mortgage',
  CreditCard: 'Credit Card',
  Loan: 'Loan',
  Other: 'Other'
};

/** Human-readable labels for each account class. */
export const accountClassLabel: Record<AccountClass, string> = {
  Asset: 'Asset',
  Liability: 'Liability'
};

/** Ordered list of account types with labels and class, for selects and grouping. */
export const accountTypes: ReadonlyArray<{ value: AccountType; label: string; class: AccountClass }> = [
  { value: 'Checking', label: 'Checking', class: 'Asset' },
  { value: 'Savings', label: 'Savings', class: 'Asset' },
  { value: 'Cash', label: 'Cash', class: 'Asset' },
  { value: 'Brokerage', label: 'Brokerage', class: 'Asset' },
  { value: 'Retirement', label: 'Retirement', class: 'Asset' },
  { value: 'Hsa', label: 'HSA', class: 'Asset' },
  { value: 'Crypto', label: 'Crypto', class: 'Asset' },
  { value: 'RealEstate', label: 'Real Estate', class: 'Asset' },
  { value: 'Vehicle', label: 'Vehicle', class: 'Asset' },
  { value: 'CreditCard', label: 'Credit Card', class: 'Liability' },
  { value: 'Loan', label: 'Loan', class: 'Liability' },
  { value: 'Mortgage', label: 'Mortgage', class: 'Liability' },
  { value: 'Other', label: 'Other', class: 'Asset' }
];

/** Stable display order for account-type groups (assets first, then liabilities). */
export const accountTypeOrder: ReadonlyArray<AccountType> = accountTypes.map(t => t.value);

/** The default class implied by a given account type. */
export function defaultClassForType(type: AccountType): AccountClass {
  return accountTypes.find(t => t.value === type)?.class ?? 'Asset';
}

/**
 * Tailwind background class used for an account type's legend dot / bar segment in
 * the assets & liabilities summary.
 */
export const accountTypeColor: Record<AccountType, string> = {
  Checking: 'bg-emerald-400',
  Savings: 'bg-teal-400',
  Cash: 'bg-green-400',
  Brokerage: 'bg-sky-400',
  Retirement: 'bg-indigo-400',
  Hsa: 'bg-cyan-400',
  Crypto: 'bg-amber-400',
  RealEstate: 'bg-violet-400',
  Vehicle: 'bg-fuchsia-400',
  CreditCard: 'bg-rose-400',
  Loan: 'bg-orange-400',
  Mortgage: 'bg-red-400',
  Other: 'bg-slate-400'
};

/** A financial account owned by the current user. */
export interface Account {
  id: number;
  userId: number;
  name: string;
  type: AccountType;
  class: AccountClass;
  currency: string;
  openingBalance: number;
  currentBalance: number;
  isManual: boolean;
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

// Request models

/** Payload to create a new account. */
export interface CreateAccountRequest {
  name: string;
  type: AccountType;
  class: AccountClass;
  currency: string;
  openingBalance: number;
  currentBalance: number;
  isManual: boolean;
  metadata?: Record<string, unknown> | null;
}

/** Payload to fully update an existing account. */
export type UpdateAccountRequest = CreateAccountRequest;

// Query parameters

/** Query parameters for listing accounts. */
export interface AccountQueryParameters extends CursorPaginationQueryParameters {
  type?: AccountType;
  class?: AccountClass;
  currency?: string;
  isManual?: boolean;
  name?: string;
}

// Balance history

/** The time window for net-worth and balance history queries. */
export type BalanceHistoryTimeframe = 'OneMonth' | 'ThreeMonths' | 'OneYear' | 'All';

/** Timeframe options with human-readable labels for selects. */
export const balanceHistoryTimeframes: ReadonlyArray<{ value: BalanceHistoryTimeframe; label: string }> = [
  { value: 'OneMonth', label: '1 month' },
  { value: 'ThreeMonths', label: '3 months' },
  { value: 'OneYear', label: '1 year' },
  { value: 'All', label: 'All time' }
];

/** Short "x change" label for a timeframe, shown next to the net-worth change. */
export const balanceHistoryChangeLabel: Record<BalanceHistoryTimeframe, string> = {
  OneMonth: '1 month change',
  ThreeMonths: '3 month change',
  OneYear: '1 year change',
  All: 'all-time change'
};

/** A single end-of-day point in a net-worth time series. */
export interface NetWorthPoint {
  asOf: string;
  netWorth: number;
  totalAssets: number;
  totalLiabilities: number;
}

/** Net-worth history: current totals, change over the window, and the end-of-day series. */
export interface NetWorthHistory {
  currency: string;
  netWorth: number;
  totalAssets: number;
  totalLiabilities: number;
  change: number;
  changePercent: number;
  series: NetWorthPoint[];
}

/** A single end-of-day point in an account's balance history. */
export interface AccountBalancePoint {
  asOf: string;
  balance: number;
}

/** An account's balance history over a window, for sparklines and change badges. */
export interface AccountHistory {
  accountId: number;
  points: AccountBalancePoint[];
}
