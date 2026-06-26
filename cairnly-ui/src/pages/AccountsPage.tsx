import { NetWorthCard } from '../components/accounts/NetWorthCard';
import { AccountGroup } from '../components/accounts/AccountGroup';
import { AccountSummaryCard } from '../components/accounts/AccountSummaryCard';
import { AccountsHeaderActions } from '../components/accounts/AccountsHeaderActions';
import { usePageHeader } from '../hooks/usePageHeader';
import {
  mockAccountGroups,
  mockAssets,
  mockCurrency,
  mockLiabilities,
  mockNetWorth,
  mockNetWorthSeries
} from '../constants/mockAccounts';

/** Stable header config (no per-render state) for the Accounts page. */
const accountsHeader = { title: 'Accounts', actions: <AccountsHeaderActions /> };

/**
 * The Accounts page: net worth overview, grouped account balances, and an
 * asset/liability summary. Currently backed by placeholder data — there is no
 * accounts API yet.
 */
export function AccountsPage() {
  usePageHeader(accountsHeader);

  return (
    <div className="space-y-6">
      <NetWorthCard
        total={mockNetWorth.total}
        change={mockNetWorth.change}
        changePercent={mockNetWorth.changePercent}
        series={mockNetWorthSeries}
        currency={mockCurrency}
      />

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <div className="space-y-4 xl:col-span-2">
          {mockAccountGroups.map(group => (
            <AccountGroup key={group.id} group={group} currency={mockCurrency} />
          ))}
        </div>
        <div className="xl:col-span-1">
          <AccountSummaryCard assets={mockAssets} liabilities={mockLiabilities} currency={mockCurrency} />
        </div>
      </div>
    </div>
  );
}
