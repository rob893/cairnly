import {
  accountTypeColor,
  accountTypeLabel,
  accountTypeOrder
} from '../types/accounts';
import type { Account, AccountBalancePoint, AccountType } from '../types/accounts';

/** A single account prepared for display within a group. */
export interface AccountRowView {
  account: Account;
  typeLabel: string;
  /** Current balance in minor units. */
  balance: number;
  /** Balance history points (minor units) for the trend sparkline. */
  trend: number[];
  /** Change versus the earliest point in the window, in minor units. */
  change: number;
  /** Change as a percentage of the baseline. */
  changePercent: number;
}

/** A group of accounts of the same type with aggregate totals and change. */
export interface AccountGroupView {
  type: AccountType;
  label: string;
  accounts: AccountRowView[];
  /** Group total current balance in minor units. */
  total: number;
  /** Group change over the window in minor units. */
  change: number;
  /** Group change as a percentage of the baseline. */
  changePercent: number;
}

/** A row in the assets / liabilities summary breakdown. */
export interface SummaryItem {
  label: string;
  /** Amount in minor units. */
  amount: number;
  /** Tailwind background class for the legend dot / bar segment. */
  color: string;
}

function percentOf(change: number, baseline: number): number {
  return baseline !== 0 ? (change / Math.abs(baseline)) * 100 : 0;
}

/**
 * Builds an account row view, deriving the trend and window change from the
 * account's balance history points (earliest-in-window baseline vs current).
 */
function buildRow(account: Account, points: AccountBalancePoint[]): AccountRowView {
  const trend = points.map(point => point.balance);
  const baseline = points.length > 0 ? points[0].balance : account.currentBalance;
  const change = account.currentBalance - baseline;

  return {
    account,
    typeLabel: accountTypeLabel[account.type],
    balance: account.currentBalance,
    trend,
    change,
    changePercent: percentOf(change, baseline)
  };
}

/**
 * Groups accounts by type (in {@link accountTypeOrder}) and computes per-group
 * totals and window change. Accounts are sorted by current balance descending
 * within each group.
 *
 * @param accounts The current user's accounts.
 * @param historyByAccountId Per-account balance history points keyed by account ID.
 */
export function buildAccountGroups(
  accounts: Account[],
  historyByAccountId: Map<number, AccountBalancePoint[]>
): AccountGroupView[] {
  const byType = new Map<AccountType, AccountRowView[]>();

  for (const account of accounts) {
    const row = buildRow(account, historyByAccountId.get(account.id) ?? []);
    const rows = byType.get(account.type);

    if (rows) {
      rows.push(row);
    } else {
      byType.set(account.type, [row]);
    }
  }

  return accountTypeOrder
    .filter(type => byType.has(type))
    .map(type => {
      const rows = (byType.get(type) ?? []).sort((a, b) => b.balance - a.balance);
      const total = rows.reduce((sum, row) => sum + row.balance, 0);
      const change = rows.reduce((sum, row) => sum + row.change, 0);

      return {
        type,
        label: accountTypeLabel[type],
        accounts: rows,
        total,
        change,
        changePercent: percentOf(change, total - change)
      };
    });
}

/**
 * Builds the assets and liabilities breakdown for the summary card by summing
 * current balances per account type within each class.
 *
 * @param accounts The current user's accounts.
 */
export function buildSummary(accounts: Account[]): { assets: SummaryItem[]; liabilities: SummaryItem[] } {
  const assetTotals = new Map<AccountType, number>();
  const liabilityTotals = new Map<AccountType, number>();

  for (const account of accounts) {
    const target = account.class === 'Asset' ? assetTotals : liabilityTotals;
    target.set(account.type, (target.get(account.type) ?? 0) + account.currentBalance);
  }

  const toItems = (totals: Map<AccountType, number>): SummaryItem[] =>
    accountTypeOrder
      .filter(type => totals.has(type))
      .map(type => ({
        label: accountTypeLabel[type],
        amount: totals.get(type) ?? 0,
        color: accountTypeColor[type]
      }));

  return { assets: toItems(assetTotals), liabilities: toItems(liabilityTotals) };
}
