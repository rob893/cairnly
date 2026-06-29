import { useCallback, useEffect, useMemo, useState } from 'react';
import { Button, Modal, Spinner } from '@heroui/react';
import { Wallet } from 'lucide-react';
import { NetWorthCard } from '../components/accounts/NetWorthCard';
import { AccountGroup } from '../components/accounts/AccountGroup';
import { AccountSummaryCard } from '../components/accounts/AccountSummaryCard';
import { AccountsHeaderActions } from '../components/accounts/AccountsHeaderActions';
import { AccountFormModal } from '../components/accounts/AccountFormModal';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { EmptyState } from '../components/EmptyState';
import { usePageHeader } from '../hooks/usePageHeader';
import { showErrorDetails } from '../utils/environment';
import { buildAccountGroups, buildSummary } from '../utils/accounts';
import { showSuccessToast } from '../utils/notifications';
import {
  useAccountHistory,
  useAccounts,
  useCreateAccount,
  useDeleteAccount,
  useNetWorthHistory,
  useUpdateAccount
} from '../hooks/accounts';
import type { Account, AccountBalancePoint, BalanceHistoryTimeframe, CreateAccountRequest } from '../types/accounts';

const DEFAULT_CURRENCY = 'USD';

/**
 * The Accounts page: a net-worth overview with history chart, account balances
 * grouped by type, and an asset/liability summary. Backed by the accounts API
 * with per-account balance history.
 */
export function AccountsPage() {
  const [timeframe, setTimeframe] = useState<BalanceHistoryTimeframe>('OneMonth');
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Account | undefined>(undefined);
  const [deleteTarget, setDeleteTarget] = useState<Account | undefined>(undefined);

  const accountsQuery = useAccounts();
  const netWorthQuery = useNetWorthHistory(timeframe);
  const accountHistoryQuery = useAccountHistory(timeframe);

  const createAccount = useCreateAccount();
  const updateAccount = useUpdateAccount();
  const deleteAccount = useDeleteAccount();

  // The list is small; eagerly pull every page so all accounts are available.
  useEffect(() => {
    if (accountsQuery.hasNextPage && !accountsQuery.isFetchingNextPage) {
      void accountsQuery.fetchNextPage();
    }
  }, [accountsQuery.hasNextPage, accountsQuery.isFetchingNextPage, accountsQuery]);

  const accounts = useMemo<Account[]>(
    () => accountsQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [],
    [accountsQuery.data]
  );

  const historyByAccountId = useMemo<Map<number, AccountBalancePoint[]>>(() => {
    const map = new Map<number, AccountBalancePoint[]>();
    for (const history of accountHistoryQuery.data ?? []) {
      map.set(history.accountId, history.points);
    }
    return map;
  }, [accountHistoryQuery.data]);

  const currency = accounts[0]?.currency ?? netWorthQuery.data?.currency ?? DEFAULT_CURRENCY;

  const groups = useMemo(() => buildAccountGroups(accounts, historyByAccountId), [accounts, historyByAccountId]);
  const summary = useMemo(() => buildSummary(accounts), [accounts]);

  const openCreate = useCallback(() => {
    setEditing(undefined);
    setFormOpen(true);
  }, []);

  const openEdit = useCallback((account: Account) => {
    setEditing(account);
    setFormOpen(true);
  }, []);

  const openDelete = useCallback((account: Account) => setDeleteTarget(account), []);

  const handleRefresh = useCallback(() => {
    void accountsQuery.refetch();
    void netWorthQuery.refetch();
    void accountHistoryQuery.refetch();
  }, [accountsQuery, netWorthQuery, accountHistoryQuery]);

  // Clear stale mutation errors whenever the form opens.
  useEffect(() => {
    if (!formOpen) {
      return;
    }
    if (editing) {
      updateAccount.reset();
    } else {
      createAccount.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formOpen, editing]);

  const isRefreshing = accountsQuery.isFetching || netWorthQuery.isFetching || accountHistoryQuery.isFetching;

  const actions = useMemo(
    () => <AccountsHeaderActions onAdd={openCreate} onRefresh={handleRefresh} isRefreshing={isRefreshing} />,
    [openCreate, handleRefresh, isRefreshing]
  );

  usePageHeader(useMemo(() => ({ title: 'Accounts', actions }), [actions]));

  const handleSubmit = async (payload: CreateAccountRequest) => {
    if (editing) {
      await updateAccount.mutateAsync({ id: editing.id, request: payload });
    } else {
      await createAccount.mutateAsync(payload);
    }

    showSuccessToast('Account saved');
    setFormOpen(false);
    setEditing(undefined);
  };

  const handleDelete = async () => {
    if (!deleteTarget) {
      return;
    }

    await deleteAccount.mutateAsync(deleteTarget.id);
    showSuccessToast('Account deleted');
    setDeleteTarget(undefined);
  };

  return (
    <div className="space-y-6">
      <NetWorthCard
        history={netWorthQuery.data}
        currency={currency}
        timeframe={timeframe}
        onTimeframeChange={setTimeframe}
        isLoading={netWorthQuery.isLoading}
      />

      {accountsQuery.isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner size="lg" color="accent" />
        </div>
      ) : accountsQuery.isError ? (
        <ApiErrorDisplay
          error={accountsQuery.error as Error}
          title="Failed to load accounts"
          showDetails={showErrorDetails}
        />
      ) : accounts.length === 0 ? (
        <EmptyState
          icon={<Wallet className="size-5" />}
          title="No accounts yet"
          subtitle="Add one to start tracking your net worth."
          cta={{ label: 'Add account', onPress: openCreate }}
        />
      ) : (
        <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
          <div className="space-y-4 xl:col-span-2">
            {groups.map(group => (
              <AccountGroup
                key={group.type}
                group={group}
                currency={currency}
                onEdit={openEdit}
                onDelete={openDelete}
              />
            ))}
          </div>
          <div className="xl:col-span-1">
            <AccountSummaryCard assets={summary.assets} liabilities={summary.liabilities} currency={currency} />
          </div>
        </div>
      )}

      <AccountFormModal
        defaultCurrency={currency}
        isOpen={formOpen}
        onOpenChange={setFormOpen}
        item={editing}
        onSubmit={handleSubmit}
        isPending={createAccount.isPending || updateAccount.isPending}
        error={(editing ? updateAccount.error : createAccount.error) as Error | null}
      />

      <Modal isOpen={deleteTarget !== undefined} onOpenChange={open => !open && setDeleteTarget(undefined)}>
        <Modal.Backdrop>
          <Modal.Container size="sm">
            <Modal.Dialog>
              <Modal.CloseTrigger />
              <Modal.Header>
                <Modal.Heading>Delete account?</Modal.Heading>
              </Modal.Header>
              <Modal.Body className="space-y-4">
                {deleteAccount.error && (
                  <ApiErrorDisplay
                    error={deleteAccount.error as Error}
                    title="Delete failed"
                    showDetails={showErrorDetails}
                  />
                )}
                <p className="text-sm text-muted">
                  Permanently delete <span className="font-medium text-foreground">{deleteTarget?.name}</span> and its
                  transactions? This cannot be undone.
                </p>
              </Modal.Body>
              <Modal.Footer>
                <Button slot="close" variant="outline">
                  Cancel
                </Button>
                <Button variant="danger" onPress={handleDelete} isPending={deleteAccount.isPending}>
                  Delete
                </Button>
              </Modal.Footer>
            </Modal.Dialog>
          </Modal.Container>
        </Modal.Backdrop>
      </Modal>
    </div>
  );
}
