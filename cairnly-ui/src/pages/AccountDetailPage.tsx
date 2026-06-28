import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router';
import { Button, Spinner } from '@heroui/react';
import { AccountBalanceCard } from '../components/accounts/AccountBalanceCard';
import { AccountDetailSummaryCard } from '../components/accounts/AccountDetailSummaryCard';
import { AccountFormModal } from '../components/accounts/AccountFormModal';
import { TransactionsTable } from '../components/transactions/TransactionsTable';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { usePageHeader } from '../hooks/usePageHeader';
import { showErrorDetails } from '../utils/environment';
import { useAccount, useAccountHistory, useUpdateAccount } from '../hooks/accounts';
import { useAccountTransactions } from '../hooks/transactions';
import type { BalanceHistoryTimeframe, CreateAccountRequest } from '../types/accounts';

/**
 * The account detail page: a balance-history chart for the account, its
 * transactions (inline-editable), and a summary panel. Reached by clicking an
 * account on the Accounts page.
 */
export function AccountDetailPage() {
  const params = useParams<{ accountId: string }>();
  const accountId = params.accountId ? Number(params.accountId) : undefined;
  const validId = typeof accountId === 'number' && Number.isFinite(accountId) ? accountId : undefined;

  const [timeframe, setTimeframe] = useState<BalanceHistoryTimeframe>('All');
  const [editOpen, setEditOpen] = useState(false);

  const accountQuery = useAccount(validId);
  const historyQuery = useAccountHistory(timeframe);
  const transactionsQuery = useAccountTransactions(validId);
  const updateAccount = useUpdateAccount();

  const account = accountQuery.data;

  const points = useMemo(
    () => historyQuery.data?.find(history => history.accountId === validId)?.points ?? [],
    [historyQuery.data, validId]
  );

  const transactionCount = useMemo(
    () => (transactionsQuery.data ? transactionsQuery.data.pages.flatMap(page => page.nodes ?? []).length : undefined),
    [transactionsQuery.data]
  );

  const header = useMemo(
    () => ({
      title: (
        <span className="flex items-center gap-2">
          <Link
            to="/accounts"
            aria-label="Back to accounts"
            className="text-muted no-underline transition-colors hover:text-foreground"
          >
            ←
          </Link>
          {account?.name ?? 'Account'}
        </span>
      ),
      actions: account ? (
        <Button variant="outline" size="sm" onPress={() => setEditOpen(true)}>
          Edit
        </Button>
      ) : undefined
    }),
    [account]
  );

  usePageHeader(header);

  const handleEditSubmit = async (payload: CreateAccountRequest) => {
    if (!validId) {
      return;
    }

    await updateAccount.mutateAsync({ id: validId, request: payload });
    setEditOpen(false);
  };

  if (validId === undefined) {
    return <p className="text-center text-sm text-muted">Invalid account.</p>;
  }

  if (accountQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" color="accent" />
      </div>
    );
  }

  if (accountQuery.isError || !account) {
    return (
      <div className="space-y-4">
        <Link to="/accounts" className="text-sm font-medium text-accent no-underline">
          ← Back to accounts
        </Link>
        <ApiErrorDisplay
          error={(accountQuery.error as Error) ?? new Error('Account not found.')}
          title="Failed to load account"
          showDetails={showErrorDetails}
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <AccountBalanceCard
        currentBalance={account.currentBalance}
        currency={account.currency}
        points={points}
        timeframe={timeframe}
        onTimeframeChange={setTimeframe}
        isLoading={historyQuery.isLoading}
      />

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <div className="xl:col-span-2">
          <TransactionsTable
            filters={{ accountId: validId }}
            currency={account.currency}
            lockedAccountId={validId}
            showAccount={false}
          />
        </div>
        <div className="xl:col-span-1">
          <AccountDetailSummaryCard account={account} transactionCount={transactionCount} />
        </div>
      </div>

      <AccountFormModal
        defaultCurrency={account.currency}
        isOpen={editOpen}
        onOpenChange={setEditOpen}
        item={account}
        onSubmit={handleEditSubmit}
        isPending={updateAccount.isPending}
        error={updateAccount.error as Error | null}
      />
    </div>
  );
}
