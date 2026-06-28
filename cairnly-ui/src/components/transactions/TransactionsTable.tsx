import { forwardRef, memo, useCallback, useEffect, useImperativeHandle, useMemo, useState } from 'react';
import { Button, Chip, Modal, Spinner } from '@heroui/react';
import { Pencil, Plus, Scale, Trash2 } from 'lucide-react';
import { CategorySelect } from '../CategorySelect';
import { TransactionFormModal } from './TransactionFormModal';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import { formatMoney, minorToMajor, parseMoneyToMinor } from '../../utils/money';
import { formatLongDate } from '../../utils/datetime';
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

/** Builds a full update request from an existing transaction plus patched fields. */
function buildUpdateRequest(transaction: Transaction, patch: Partial<UpdateTransactionRequest>): UpdateTransactionRequest {
  return {
    accountId: transaction.accountId,
    date: transaction.date,
    amount: transaction.amount,
    merchant: transaction.merchant ?? null,
    description: transaction.description ?? null,
    categoryId: transaction.categoryId,
    source: transaction.source,
    parentTransactionId: transaction.parentTransactionId ?? null,
    tagIds: transaction.tagIds,
    metadata: transaction.metadata,
    ...patch
  };
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
 * A reusable, editable list of transactions grouped by day. Loads every matching
 * page client-side, supports inline editing of merchant, category, and amount, and
 * full create/edit via a modal plus delete. Used on the account detail page
 * (scoped to one account) and the global Transactions page.
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

  // Eagerly pull every page so the grouped list is complete.
  useEffect(() => {
    if (query.hasNextPage && !query.isFetchingNextPage) {
      void query.fetchNextPage();
    }
  }, [query.hasNextPage, query.isFetchingNextPage, query]);

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

  const baseCurrency = currency ?? accounts[0]?.currency ?? 'USD';

  const updateAsync = updateTransaction.mutateAsync;

  const handleInlineSave = useCallback(
    async (transaction: Transaction, patch: Partial<UpdateTransactionRequest>) => {
      await updateAsync({ id: transaction.id, request: buildUpdateRequest(transaction, patch) });
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

    setFormOpen(false);
    setEditing(undefined);
  };

  const handleDelete = async () => {
    if (!deleteTarget) {
      return;
    }

    await deleteTransaction.mutateAsync(deleteTarget.id);
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
        <p className="py-8 text-center text-sm text-muted">No transactions yet. Add one to get started.</p>
      ) : (
        <div className="overflow-hidden rounded-2xl border border-border bg-surface">
          {groups.map(group => {
            const groupTotal = amountDisplay(group.total, baseCurrency);

            return (
              <section key={group.key}>
                <div className="flex items-center justify-between gap-3 bg-surface-secondary/60 px-4 py-2 text-sm">
                  <span className="font-medium text-muted">{group.label}</span>
                  <span className={`tabular-nums ${groupTotal.className}`}>{groupTotal.text}</span>
                </div>

                <ul className="divide-y divide-border">
                  {group.transactions.map(transaction => (
                    <TransactionRow
                      key={transaction.id}
                      transaction={transaction}
                      currency={baseCurrency}
                      categoriesById={categoriesById}
                      accountName={showAccount ? accountsById.get(transaction.accountId)?.name : undefined}
                      onInlineSave={handleInlineSave}
                      onEdit={openEdit}
                      onDelete={openDelete}
                    />
                  ))}
                </ul>
              </section>
            );
          })}
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
                <p className="text-sm text-muted">
                  Permanently delete this transaction? This cannot be undone.
                </p>
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
    <li className="grid grid-cols-[1fr_auto] items-center gap-x-4 gap-y-1 px-4 py-3 transition-colors hover:bg-surface-secondary/40 lg:grid-cols-[2fr_1.5fr_1fr_1fr_auto]">
      <div className="flex min-w-0 items-center gap-3">
        <span
          className="flex size-8 shrink-0 items-center justify-center rounded-full bg-surface-secondary text-sm"
          aria-hidden="true"
        >
          {isAdjustment ? <Scale className="size-4 text-muted" /> : category?.icon || (transaction.merchant || transaction.description || '?')[0]?.toUpperCase()}
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
          <EditableCategoryCell transaction={transaction} category={category} onSave={onInlineSave} />
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
    </li>
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
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState(current);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!editing) {
      setValue(current);
    }
  }, [current, editing]);

  const cancel = () => {
    setValue(current);
    setEditing(false);
  };

  const commit = async () => {
    const trimmed = value.trim();
    if (!trimmed || trimmed === current) {
      cancel();
      return;
    }
    setSaving(true);
    try {
      await onSave(transaction, { merchant: trimmed });
      setEditing(false);
    } catch {
      setValue(current);
    } finally {
      setSaving(false);
    }
  };

  if (!editing) {
    const label = transaction.merchant || transaction.description || fallback || 'Transaction';
    return (
      <button
        type="button"
        onClick={() => setEditing(true)}
        aria-label="Edit merchant"
        className="-mx-1 max-w-full truncate rounded px-1 text-left font-medium text-foreground transition-colors hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
      >
        {label}
      </button>
    );
  }

  return (
    <input
      type="text"
      autoFocus
      value={value}
      disabled={saving}
      aria-label="Merchant"
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
      className="w-full min-w-32 rounded border border-border bg-surface px-2 py-1 text-sm font-medium outline-none focus-visible:ring-2 focus-visible:ring-focus"
    />
  );
}

/** Category cell that switches to an inline picker on click; saves on selection. */
function EditableCategoryCell({
  transaction,
  category,
  onSave
}: {
  transaction: Transaction;
  category: Category | undefined;
  onSave(transaction: Transaction, patch: Partial<UpdateTransactionRequest>): Promise<void>;
}) {
  const [editing, setEditing] = useState(false);

  if (!editing) {
    return (
      <button
        type="button"
        onClick={() => setEditing(true)}
        aria-label="Edit category"
        className="-mx-1 flex max-w-full items-center gap-1.5 rounded px-1 text-left text-sm text-muted transition-colors hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
      >
        {category?.icon && <span aria-hidden="true">{category.icon}</span>}
        <span className="truncate">{category?.name || '—'}</span>
      </button>
    );
  }

  return (
    <CategorySelect
      aria-label="Category"
      value={transaction.categoryId}
      defaultOpen
      onChange={categoryId => {
        if (categoryId !== transaction.categoryId) {
          void onSave(transaction, { categoryId });
        }
        setEditing(false);
      }}
      onOpenChange={open => {
        if (!open) {
          setEditing(false);
        }
      }}
      className="w-full min-w-44"
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
