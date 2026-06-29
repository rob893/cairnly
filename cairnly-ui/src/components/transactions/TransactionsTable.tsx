import { forwardRef, memo, useCallback, useEffect, useImperativeHandle, useMemo, useRef, useState } from 'react';
import { Button, Chip, Modal, Spinner } from '@heroui/react';
import { useVirtualizer } from '@tanstack/react-virtual';
import { Pencil, Plus, Scale, Trash2 } from 'lucide-react';
import { EditableCategoryCell } from '../EditableCategoryCell';
import { EditableTextCell } from '../EditableTextCell';
import { TransactionFormModal } from './TransactionFormModal';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { EmptyState } from '../EmptyState';
import { showErrorDetails } from '../../utils/environment';
import { formatMoney, minorToMajor, parseMoneyToMinor } from '../../utils/money';
import { formatLongDate } from '../../utils/datetime';
import { showSuccessToast } from '../../utils/notifications';
import { buildTransactionUpdateRequest } from '../../utils/patchRequest';
import { useAccounts } from '../../hooks/accounts';
import { useCategories } from '../../hooks/categories';
import {
  useCreateTransaction,
  useDeleteTransaction,
  useTransactions,
  useUpdateTransaction,
  type TransactionFilters
} from '../../hooks/transactions';
import type { Account } from '../../types/accounts';
import type { Category } from '../../types/categories';
import type { CreateTransactionRequest, Transaction, UpdateTransactionRequest } from '../../types/transactions';

interface TransactionsTableProps {
  /** Filters scoping the list (e.g. `{ accountId }`). Omit to list all. */
  filters?: TransactionFilters;
  /** Currency used to format amounts and group totals. Defaults to the first loaded account's. */
  currency?: string;
  /** Show the account column (defaults to true unless scoped to one account). */
  showAccount?: boolean;
  /** When set, new transactions are scoped to this account and its field is locked. */
  lockedAccountId?: number;
  /** Heading shown in the toolbar. */
  title?: string;
  /** Whether to render the in-table toolbar (heading + add button). Defaults to true. */
  showToolbar?: boolean;
}

/** Imperative handle exposing the table's create flow so a parent can trigger it. */
export interface TransactionsTableHandle {
  openCreate(): void;
}

interface TransactionGroup {
  key: string;
  label: string;
  total: number;
  transactions: Transaction[];
}

type TransactionListItem =
  | { type: 'group'; key: string; group: TransactionGroup }
  | { type: 'transaction'; key: string; transaction: Transaction };

function estimateTransactionListItemSize(item: TransactionListItem): number {
  return item.type === 'group' ? 38 : 66;
}

/** Formats a signed minor-unit amount: inflows are prefixed with `+` and tinted. */
function amountDisplay(amount: number, currency: string): { text: string; className: string } {
  if (amount > 0) {
    return { text: `+${formatMoney(amount, currency)}`, className: 'text-success' };
  }

  return { text: formatMoney(amount, currency), className: 'text-foreground' };
}

/** Groups transactions by calendar day (descending) with a per-day net total. */
function groupByDay(transactions: Transaction[]): TransactionGroup[] {
  const groups = new Map<string, TransactionGroup>();

  for (const transaction of transactions) {
    const key = transaction.date.slice(0, 10);
    const existing = groups.get(key);

    if (existing) {
      existing.transactions.push(transaction);
      existing.total += transaction.amount;
    } else {
      groups.set(key, {
        key,
        label: formatLongDate(transaction.date),
        total: transaction.amount,
        transactions: [transaction]
      });
    }
  }

  return [...groups.values()].sort((a, b) => b.key.localeCompare(a.key));
}

/**
 * A reusable, editable list of transactions grouped by day. Pages are loaded on
 * demand and the loaded rows are virtualized, while inline edits, modal
 * create/edit, and delete remain available.
 */
export const TransactionsTable = forwardRef<TransactionsTableHandle, TransactionsTableProps>(function TransactionsTable(
  { filters, currency, showAccount, lockedAccountId, title = 'Transactions', showToolbar = true },
  ref
) {
  const resolvedFilters = useMemo<TransactionFilters>(() => filters ?? {}, [filters]);
  const query = useTransactions(resolvedFilters);
  const accountsQuery = useAccounts();
  const { categoriesById } = useCategories();

  const createTransaction = useCreateTransaction();
  const updateTransaction = useUpdateTransaction();
  const deleteTransaction = useDeleteTransaction();

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Transaction | undefined>(undefined);
  const [deleteTarget, setDeleteTarget] = useState<Transaction | undefined>(undefined);
  const virtualListRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (accountsQuery.hasNextPage && !accountsQuery.isFetchingNextPage) {
      void accountsQuery.fetchNextPage();
    }
  }, [accountsQuery.hasNextPage, accountsQuery.isFetchingNextPage, accountsQuery]);

  const transactions = useMemo<Transaction[]>(
    () => query.data?.pages.flatMap(page => page.nodes ?? []) ?? [],
    [query.data]
  );

  const accounts = useMemo<Account[]>(
    () => accountsQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [],
    [accountsQuery.data]
  );

  const accountsById = useMemo(() => {
    const map = new Map<number, Account>();
    for (const account of accounts) {
      map.set(account.id, account);
    }
    return map;
  }, [accounts]);

  const groups = useMemo(() => groupByDay(transactions), [transactions]);
  const listItems = useMemo<TransactionListItem[]>(
    () =>
      groups.flatMap(group => [
        { type: 'group' as const, key: `group-${group.key}`, group },
        ...group.transactions.map(transaction => ({
          type: 'transaction' as const,
          key: `transaction-${transaction.id}`,
          transaction
        }))
      ]),
    [groups]
  );

  const baseCurrency = currency ?? accounts[0]?.currency ?? 'USD';
  // TanStack Virtual exposes imperative helpers that React Compiler cannot memoize.
  // eslint-disable-next-line react-hooks/incompatible-library
  const rowVirtualizer = useVirtualizer({
    count: listItems.length,
    getScrollElement: () => virtualListRef.current,
    estimateSize: index => (listItems[index] ? estimateTransactionListItemSize(listItems[index]) : 66),
    initialRect: { width: 0, height: 600 },
    overscan: 8
  });
  const fallbackVirtualItems = useMemo(() => {
    let offset = 0;
    return listItems.slice(0, 20).map((item, index) => {
      const size = estimateTransactionListItemSize(item);
      const start = offset;
      offset += size;
      return { index, key: item.key, start, size, end: offset, lane: 0 };
    });
  }, [listItems]);
  const measuredVirtualItems = rowVirtualizer.getVirtualItems();
  const virtualItems = measuredVirtualItems.length > 0 ? measuredVirtualItems : fallbackVirtualItems;
  const virtualHeight =
    rowVirtualizer.getTotalSize() ||
    listItems.reduce((total, item) => total + estimateTransactionListItemSize(item), 0);

  const updateAsync = updateTransaction.mutateAsync;

  const handleInlineSave = useCallback(
    async (transaction: Transaction, patch: Partial<UpdateTransactionRequest>) => {
      await updateAsync({ id: transaction.id, request: buildTransactionUpdateRequest(transaction, patch) });
      showSuccessToast('Transaction saved');
    },
    [updateAsync]
  );

  const openCreate = useCallback(() => {
    setEditing(undefined);
    setFormOpen(true);
  }, []);

  useImperativeHandle(ref, () => ({ openCreate }), [openCreate]);

  const openEdit = useCallback((transaction: Transaction) => {
    setEditing(transaction);
    setFormOpen(true);
  }, []);

  const openDelete = useCallback((transaction: Transaction) => setDeleteTarget(transaction), []);

  // Clear stale mutation errors whenever the form opens.
  useEffect(() => {
    if (!formOpen) {
      return;
    }
    if (editing) {
      updateTransaction.reset();
    } else {
      createTransaction.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formOpen, editing]);

  const handleSubmit = async (payload: CreateTransactionRequest) => {
    if (editing) {
      await updateTransaction.mutateAsync({ id: editing.id, request: payload });
    } else {
      await createTransaction.mutateAsync(payload);
    }

    showSuccessToast('Transaction saved');
    setFormOpen(false);
    setEditing(undefined);
  };

  const handleDelete = async () => {
    if (!deleteTarget) {
      return;
    }

    await deleteTransaction.mutateAsync(deleteTarget.id);
    showSuccessToast('Transaction deleted');
    setDeleteTarget(undefined);
  };

  return (
    <div className="space-y-3">
      {showToolbar && (
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-lg font-semibold">{title}</h2>
          <Button size="sm" onPress={openCreate}>
            <Plus className="size-4" />
            Add transaction
          </Button>
        </div>
      )}

      {query.isLoading ? (
        <div className="flex justify-center py-10">
          <Spinner size="lg" color="accent" />
        </div>
      ) : query.isError ? (
        <ApiErrorDisplay
          error={query.error as Error}
          title="Failed to load transactions"
          showDetails={showErrorDetails}
        />
      ) : transactions.length === 0 ? (
        <EmptyState
          icon={<Scale className="size-5" />}
          title="No transactions yet"
          subtitle="Add one to get started tracking income and spending."
          cta={{ label: 'Add transaction', onPress: openCreate }}
        />
      ) : (
        <div className="overflow-hidden rounded-2xl border border-border bg-surface">
          <div ref={virtualListRef} className="max-h-[min(70vh,48rem)] overflow-auto" role="list">
            <div className="relative w-full" style={{ height: `${virtualHeight}px` }}>
              {virtualItems.map(virtualItem => {
                const item = listItems[virtualItem.index];

                return (
                  <div
                    key={item.key}
                    ref={rowVirtualizer.measureElement}
                    data-index={virtualItem.index}
                    className="absolute left-0 top-0 w-full"
                    style={{ transform: `translateY(${virtualItem.start}px)` }}
                  >
                    {item.type === 'group' ? (
                      <TransactionGroupHeader group={item.group} currency={baseCurrency} />
                    ) : (
                      <TransactionRow
                        transaction={item.transaction}
                        currency={baseCurrency}
                        categoriesById={categoriesById}
                        accountName={showAccount ? accountsById.get(item.transaction.accountId)?.name : undefined}
                        onInlineSave={handleInlineSave}
                        onEdit={openEdit}
                        onDelete={openDelete}
                      />
                    )}
                  </div>
                );
              })}
            </div>
          </div>
          {query.hasNextPage && (
            <div className="flex justify-center border-t border-border bg-surface px-4 py-3">
              <Button variant="outline" onPress={() => query.fetchNextPage()} isPending={query.isFetchingNextPage}>
                Load more transactions
              </Button>
            </div>
          )}
        </div>
      )}

      <TransactionFormModal
        accounts={accounts}
        lockedAccountId={lockedAccountId}
        isOpen={formOpen}
        onOpenChange={setFormOpen}
        item={editing}
        onSubmit={handleSubmit}
        isPending={createTransaction.isPending || updateTransaction.isPending}
        error={(editing ? updateTransaction.error : createTransaction.error) as Error | null}
      />

      <Modal isOpen={deleteTarget !== undefined} onOpenChange={open => !open && setDeleteTarget(undefined)}>
        <Modal.Backdrop>
          <Modal.Container size="sm">
            <Modal.Dialog>
              <Modal.CloseTrigger />
              <Modal.Header>
                <Modal.Heading>Delete transaction?</Modal.Heading>
              </Modal.Header>
              <Modal.Body className="space-y-4">
                {deleteTransaction.error && (
                  <ApiErrorDisplay
                    error={deleteTransaction.error as Error}
                    title="Delete failed"
                    showDetails={showErrorDetails}
                  />
                )}
                <p className="text-sm text-muted">Permanently delete this transaction? This cannot be undone.</p>
              </Modal.Body>
              <Modal.Footer>
                <Button slot="close" variant="outline">
                  Cancel
                </Button>
                <Button variant="danger" onPress={handleDelete} isPending={deleteTransaction.isPending}>
                  Delete
                </Button>
              </Modal.Footer>
            </Modal.Dialog>
          </Modal.Container>
        </Modal.Backdrop>
      </Modal>
    </div>
  );
});

function TransactionGroupHeader({ group, currency }: { group: TransactionGroup; currency: string }) {
  const groupTotal = amountDisplay(group.total, currency);

  return (
    <div
      className="flex items-center justify-between gap-3 bg-surface-secondary/60 px-4 py-2 text-sm"
      role="presentation"
    >
      <span className="font-medium text-muted">{group.label}</span>
      <span className={`tabular-nums ${groupTotal.className}`}>{groupTotal.text}</span>
    </div>
  );
}

interface TransactionRowProps {
  transaction: Transaction;
  currency: string;
  categoriesById: Map<number, Category>;
  accountName?: string;
  onInlineSave(transaction: Transaction, patch: Partial<UpdateTransactionRequest>): Promise<void>;
  onEdit(transaction: Transaction): void;
  onDelete(transaction: Transaction): void;
}

/** A single transaction row with inline-editable merchant, category, and amount. */
const TransactionRow = memo(function TransactionRow({
  transaction,
  currency,
  categoriesById,
  accountName,
  onInlineSave,
  onEdit,
  onDelete
}: TransactionRowProps) {
  const category = categoriesById.get(transaction.categoryId);
  const isAdjustment = transaction.isBalanceAdjustment;

  return (
    <div
      className="grid grid-cols-[1fr_auto] items-center gap-x-4 gap-y-1 border-t border-border px-4 py-3 transition-colors hover:bg-surface-secondary/40 lg:grid-cols-[2fr_1.5fr_1fr_1fr_auto]"
      role="listitem"
    >
      <div className="flex min-w-0 items-center gap-3">
        <span
          className="flex size-8 shrink-0 items-center justify-center rounded-full bg-surface-secondary text-sm"
          aria-hidden="true"
        >
          {isAdjustment ? (
            <Scale className="size-4 text-muted" />
          ) : (
            category?.icon || (transaction.merchant || transaction.description || '?')[0]?.toUpperCase()
          )}
        </span>
        {isAdjustment ? (
          <span className="truncate font-medium text-foreground">Balance adjustment</span>
        ) : (
          <EditableMerchantCell transaction={transaction} fallback={category?.name} onSave={onInlineSave} />
        )}
      </div>

      <div className="hidden min-w-0 lg:block">
        {isAdjustment ? (
          <Chip variant="soft" size="sm">
            Adjustment
          </Chip>
        ) : (
          <TransactionCategoryCell transaction={transaction} category={category} onSave={onInlineSave} />
        )}
      </div>

      <div className="hidden min-w-0 items-center gap-2 text-sm text-muted lg:flex">
        {accountName ? <span className="truncate">{accountName}</span> : <span aria-hidden="true">—</span>}
      </div>

      {isAdjustment ? (
        <div className="text-right">
          <span
            className={`text-sm font-semibold tabular-nums ${transaction.amount > 0 ? 'text-success' : 'text-foreground'}`}
          >
            {transaction.amount > 0 ? '+' : ''}
            {formatMoney(transaction.amount, currency)}
          </span>
        </div>
      ) : (
        <EditableAmountCell transaction={transaction} currency={currency} onSave={onInlineSave} />
      )}

      <div className="flex shrink-0 items-center justify-end gap-1">
        {!isAdjustment && (
          <Button
            isIconOnly
            variant="ghost"
            size="sm"
            onPress={() => onEdit(transaction)}
            aria-label="Edit transaction"
          >
            <Pencil className="size-4" />
          </Button>
        )}
        <Button
          isIconOnly
          variant="danger-soft"
          size="sm"
          onPress={() => onDelete(transaction)}
          aria-label="Delete transaction"
        >
          <Trash2 className="size-4" />
        </Button>
      </div>
    </div>
  );
});

/** Merchant cell that switches to a text input on click; saves on Enter or blur. */
function EditableMerchantCell({
  transaction,
  fallback,
  onSave
}: {
  transaction: Transaction;
  fallback?: string;
  onSave(transaction: Transaction, patch: Partial<UpdateTransactionRequest>): Promise<void>;
}) {
  const current = transaction.merchant ?? '';
  const label = transaction.merchant || transaction.description || fallback || 'Transaction';
  return (
    <EditableTextCell
      item={transaction}
      value={current}
      displayValue={label}
      editAriaLabel="Edit merchant"
      inputAriaLabel="Merchant"
      buttonClassName="-mx-1 max-w-full truncate rounded px-1 text-left font-medium text-foreground transition-colors hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
      inputClassName="w-full min-w-32 rounded border border-border bg-surface px-2 py-1 text-sm font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus"
      onSave={(item, merchant) => onSave(item, { merchant })}
    />
  );
}

/** Category cell that switches to an inline picker on click; saves on selection. */
function TransactionCategoryCell({
  transaction,
  category,
  onSave
}: {
  transaction: Transaction;
  category: Category | undefined;
  onSave(transaction: Transaction, patch: Partial<UpdateTransactionRequest>): Promise<void>;
}) {
  return (
    <EditableCategoryCell
      item={transaction}
      categoryId={transaction.categoryId}
      category={category}
      editAriaLabel="Edit category"
      buttonClassName="-mx-1 flex max-w-full items-center gap-1.5 rounded px-1 text-left text-sm text-muted transition-colors hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
      onSave={(item, categoryId) => onSave(item, { categoryId })}
    />
  );
}

/** Amount cell that switches to a signed money input on click; saves on Enter or blur. */
function EditableAmountCell({
  transaction,
  currency,
  onSave
}: {
  transaction: Transaction;
  currency: string;
  onSave(transaction: Transaction, patch: Partial<UpdateTransactionRequest>): Promise<void>;
}) {
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState(String(minorToMajor(transaction.amount, currency)));
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!editing) {
      setValue(String(minorToMajor(transaction.amount, currency)));
    }
  }, [transaction.amount, currency, editing]);

  const cancel = () => {
    setValue(String(minorToMajor(transaction.amount, currency)));
    setEditing(false);
  };

  const commit = async () => {
    const minor = parseMoneyToMinor(value, currency);
    if (minor === null || minor === transaction.amount) {
      cancel();
      return;
    }
    setSaving(true);
    try {
      await onSave(transaction, { amount: minor });
      setEditing(false);
    } catch {
      setValue(String(minorToMajor(transaction.amount, currency)));
    } finally {
      setSaving(false);
    }
  };

  if (!editing) {
    const amount = amountDisplay(transaction.amount, currency);
    return (
      <div className="text-right">
        <button
          type="button"
          onClick={() => setEditing(true)}
          aria-label="Edit amount"
          className={`-mx-1 rounded px-1 text-sm font-semibold tabular-nums transition-colors hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus ${amount.className}`}
        >
          {amount.text}
        </button>
      </div>
    );
  }

  return (
    <div className="text-right">
      <input
        type="text"
        autoFocus
        value={value}
        disabled={saving}
        aria-label="Amount"
        onChange={event => setValue(event.target.value)}
        onBlur={() => void commit()}
        onKeyDown={event => {
          if (event.key === 'Enter') {
            event.preventDefault();
            void commit();
          } else if (event.key === 'Escape') {
            event.preventDefault();
            cancel();
          }
        }}
        className="w-28 rounded border border-border bg-surface px-2 py-1 text-right text-sm font-semibold tabular-nums outline-none focus-visible:ring-2 focus-visible:ring-focus"
      />
    </div>
  );
}
