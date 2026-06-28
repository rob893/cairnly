import { memo, useCallback, useEffect, useMemo, useState } from 'react';
import type { SortDescriptor } from 'react-aria-components';
import { Button, Card, CardContent, CardHeader, Chip, Modal, Spinner, Table } from '@heroui/react';
import { LineItemFormModal } from './LineItemFormModal';
import { SelectField } from '../SelectField';
import { CategorySelect } from '../CategorySelect';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import { formatMoney } from '../../utils/money';
import { cadenceBreakdown, periodsPerYear } from '../../utils/cadence';
import { Eye, Pencil, Trash2 } from 'lucide-react';
import { useClientTable } from '../../hooks/useClientTable';
import { useTags } from '../../hooks/tags';
import {
  useSpendingPlanExpenses,
  useSpendingPlanIncomes,
  useCreateExpense,
  useCreateIncome,
  useDeleteExpense,
  useDeleteIncome,
  useUpdateExpense,
  useUpdateIncome
} from '../../hooks/spendingPlans';
import { cadenceLabel } from '../../types/spendingPlans';
import { useCategories } from '../../hooks/categories';
import type {
  SpendingPlanExpense,
  SpendingPlanIncome,
  CadenceAmounts,
  CreateSpendingPlanExpenseRequest,
  CreateSpendingPlanIncomeRequest
} from '../../types/spendingPlans';
import type { Category } from '../../types/categories';
import type { Tag } from '../../types/tags';

type LineItem = SpendingPlanIncome | SpendingPlanExpense;

interface LineItemsSectionProps {
  kind: 'income' | 'expense';
  spendingPlanId: number;
  currency: string;
}

const PAGE_SIZE_OPTIONS = [
  { value: '10', label: '10' },
  { value: '25', label: '25' },
  { value: '50', label: '50' },
  { value: '100', label: '100' }
];

const COST_COLUMNS: ReadonlyArray<{ key: keyof CadenceAmounts; label: string }> = [
  { key: 'daily', label: 'Daily Cost' },
  { key: 'weekly', label: 'Weekly Cost' },
  { key: 'monthly', label: 'Monthly Cost' },
  { key: 'annual', label: 'Annual Cost' }
];

/**
 * Lists a spendingPlan's income or expense line items in a HeroUI table with built-in
 * sortable column headers. All line items are loaded on the client (the set is
 * small) so search (including by tag name), sorting, and pagination happen
 * locally. Expense rows show the amount normalized across daily / weekly /
 * monthly / annual cadences. Per-row details open in a modal.
 */
export function LineItemsSection({ kind, spendingPlanId, currency }: LineItemsSectionProps) {
  const isIncome = kind === 'income';
  const title = isIncome ? 'Income' : 'Expenses';
  const addLabel = isIncome ? 'Add income' : 'Add expense';

  const incomesQuery = useSpendingPlanIncomes(isIncome ? spendingPlanId : undefined);
  const expensesQuery = useSpendingPlanExpenses(isIncome ? undefined : spendingPlanId);
  const query = isIncome ? incomesQuery : expensesQuery;

  const { tagsById } = useTags();
  const { categoriesById } = useCategories();

  const categoryNameOf = useMemo(
    () =>
      (item: LineItem): string =>
        categoriesById.get(item.categoryId)?.name ?? '',
    [categoriesById]
  );

  const createIncome = useCreateIncome(spendingPlanId);
  const updateIncome = useUpdateIncome(spendingPlanId);
  const deleteIncome = useDeleteIncome(spendingPlanId);
  const createExpense = useCreateExpense(spendingPlanId);
  const updateExpense = useUpdateExpense(spendingPlanId);
  const deleteExpense = useDeleteExpense(spendingPlanId);

  const createMutation = isIncome ? createIncome : createExpense;
  const updateMutation = isIncome ? updateIncome : updateExpense;
  const deleteMutation = isIncome ? deleteIncome : deleteExpense;

  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<LineItem | undefined>(undefined);
  const [deleteTarget, setDeleteTarget] = useState<LineItem | undefined>(undefined);
  const [detailsTarget, setDetailsTarget] = useState<LineItem | undefined>(undefined);

  // The list is small; eagerly pull every page so client-side search/sort/paginate sees all rows.
  useEffect(() => {
    if (query.hasNextPage && !query.isFetchingNextPage) {
      void query.fetchNextPage();
    }
  }, [query.hasNextPage, query.isFetchingNextPage, query]);

  const items = useMemo<LineItem[]>(
    () => (query.data?.pages.flatMap(page => page.nodes ?? []) ?? []) as LineItem[],
    [query.data]
  );

  const tagNamesOf = useMemo(
    () =>
      (item: LineItem): string[] =>
        item.tagIds.map(id => tagsById.get(id)?.name ?? '').filter(name => name !== ''),
    [tagsById]
  );

  const getSearchText = useMemo(
    () => (item: LineItem) =>
      [item.name, item.description ?? '', categoryNameOf(item), ...tagNamesOf(item)].join(' ').toLowerCase(),
    [tagNamesOf, categoryNameOf]
  );

  const getSortValue = useMemo(
    () =>
      (item: LineItem, key: string): string | number => {
        switch (key) {
          case 'name':
            return item.name.toLowerCase();
          case 'category':
            return categoryNameOf(item).toLowerCase();
          case 'amount':
            return item.amount;
          case 'cadence':
            return periodsPerYear(item.cadence);
          case 'daily':
          case 'weekly':
          case 'monthly':
          case 'annual':
            return cadenceBreakdown(item.amount, item.cadence)[key];
          default:
            return item.name.toLowerCase();
        }
      },
    [categoryNameOf]
  );

  const table = useClientTable(items, {
    getSearchText,
    getSortValue,
    initialSortKey: 'name',
    initialPageSize: 10
  });

  // Reset the relevant mutation's error state whenever the form opens, so a prior
  // failure doesn't linger. Kept in an effect so the row callbacks stay stable.
  useEffect(() => {
    if (!formOpen) {
      return;
    }
    if (editing) {
      updateMutation.reset();
    } else {
      createMutation.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [formOpen, editing]);

  const openCreate = () => {
    setEditing(undefined);
    setFormOpen(true);
  };

  const openEdit = useCallback((item: LineItem) => {
    setEditing(item);
    setFormOpen(true);
  }, []);

  const openDetails = useCallback((item: LineItem) => setDetailsTarget(item), []);
  const openDeletePrompt = useCallback((item: LineItem) => setDeleteTarget(item), []);

  const updateIncomeAsync = updateIncome.mutateAsync;
  const updateExpenseAsync = updateExpense.mutateAsync;

  // Persists a partial inline edit (name or category) by sending a full update
  // request built from the current item plus the patched fields.
  const handleInlineSave = useCallback(
    async (item: LineItem, patch: { name?: string; categoryId?: number }) => {
      const request = {
        name: patch.name ?? item.name,
        description: item.description ?? null,
        amount: item.amount,
        cadence: item.cadence,
        categoryId: patch.categoryId ?? item.categoryId,
        tagIds: item.tagIds
      };

      if (isIncome) {
        await updateIncomeAsync({ incomeId: item.id, request });
      } else {
        await updateExpenseAsync({ expenseId: item.id, request });
      }
    },
    [isIncome, updateIncomeAsync, updateExpenseAsync]
  );

  const handleSubmit = async (payload: CreateSpendingPlanIncomeRequest & CreateSpendingPlanExpenseRequest) => {
    const expensePayload: CreateSpendingPlanExpenseRequest = {
      name: payload.name,
      description: payload.description,
      amount: payload.amount,
      cadence: payload.cadence,
      categoryId: payload.categoryId,
      tagIds: payload.tagIds
    };

    if (editing) {
      if (isIncome) {
        await updateIncome.mutateAsync({ incomeId: editing.id, request: payload });
      } else {
        await updateExpense.mutateAsync({ expenseId: editing.id, request: expensePayload });
      }
    } else if (isIncome) {
      await createIncome.mutateAsync(payload);
    } else {
      await createExpense.mutateAsync(expensePayload);
    }

    setFormOpen(false);
    setEditing(undefined);
  };

  const handleDelete = async () => {
    if (!deleteTarget) {
      return;
    }

    if (isIncome) {
      await deleteIncome.mutateAsync(deleteTarget.id);
    } else {
      await deleteExpense.mutateAsync(deleteTarget.id);
    }

    setDeleteTarget(undefined);
  };

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-lg font-semibold shrink-0">{title}</h2>
        <div className="flex items-center gap-3 w-full sm:w-auto">
          <input
            type="search"
            value={table.search}
            onChange={event => table.setSearch(event.target.value)}
            placeholder="Search name or tag…"
            aria-label={`Search ${kind}`}
            className="w-full sm:w-64 rounded-lg border border-border bg-surface px-3 py-1.5 text-sm outline-none focus-visible:ring-2 focus-visible:ring-focus"
          />
          <Button size="sm" onPress={openCreate} className="shrink-0">
            {addLabel}
          </Button>
        </div>
      </CardHeader>
      <CardContent className="px-2 pb-4 sm:px-6 sm:pb-6">
        {query.isLoading ? (
          <div className="flex justify-center py-10">
            <Spinner size="lg" color="accent" />
          </div>
        ) : query.isError ? (
          <div className="px-4">
            <ApiErrorDisplay
              error={query.error as Error}
              title={`Failed to load ${kind}`}
              showDetails={showErrorDetails}
            />
          </div>
        ) : items.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted">
            No {kind} line items yet. Add one to start building this spending plan.
          </p>
        ) : (
          <>
            <LineItemsTable
              title={title}
              kind={kind}
              isIncome={isIncome}
              currency={currency}
              items={table.pageItems}
              searchTerm={table.search}
              sortKey={table.sortKey}
              sortDirection={table.sortDirection}
              onSortChange={table.setSort}
              categoriesById={categoriesById}
              tagsById={tagsById}
              onDetails={openDetails}
              onEdit={openEdit}
              onDelete={openDeletePrompt}
              onInlineSave={handleInlineSave}
            />

            <TableFooter table={table} />
          </>
        )}
      </CardContent>

      <LineItemFormModal
        kind={kind}
        currency={currency}
        isOpen={formOpen}
        onOpenChange={setFormOpen}
        item={editing}
        onSubmit={handleSubmit}
        isPending={createMutation.isPending || updateMutation.isPending}
        error={(editing ? updateMutation.error : createMutation.error) as Error | null}
      />

      <DetailsModal
        item={detailsTarget}
        currency={currency}
        tagNames={detailsTarget ? tagNamesOf(detailsTarget) : []}
        onClose={() => setDetailsTarget(undefined)}
        onTagClick={name => {
          table.setSearch(name);
          setDetailsTarget(undefined);
        }}
      />

      <Modal isOpen={deleteTarget !== undefined} onOpenChange={open => !open && setDeleteTarget(undefined)}>
        <Modal.Backdrop>
          <Modal.Container size="sm">
            <Modal.Dialog>
              <Modal.CloseTrigger />
              <Modal.Header>
                <Modal.Heading>Delete {kind}?</Modal.Heading>
              </Modal.Header>
              <Modal.Body className="space-y-4">
                {deleteMutation.error && (
                  <ApiErrorDisplay
                    error={deleteMutation.error as Error}
                    title="Delete failed"
                    showDetails={showErrorDetails}
                  />
                )}
                <p className="text-sm text-muted">
                  Permanently delete <span className="font-medium text-foreground">{deleteTarget?.name}</span>? This
                  cannot be undone.
                </p>
              </Modal.Body>
              <Modal.Footer>
                <Button slot="close" variant="outline">
                  Cancel
                </Button>
                <Button variant="danger" onPress={handleDelete} isPending={deleteMutation.isPending}>
                  Delete
                </Button>
              </Modal.Footer>
            </Modal.Dialog>
          </Modal.Container>
        </Modal.Backdrop>
      </Modal>
    </Card>
  );
}

type TableApi = ReturnType<typeof useClientTable<LineItem>>;

/** A sortable HeroUI table column with a sort indicator driven by the column's render props. */
function SortableColumn({
  id,
  label,
  align = 'left',
  isRowHeader
}: {
  id: string;
  label: string;
  align?: 'left' | 'right';
  isRowHeader?: boolean;
}) {
  return (
    <Table.Column
      id={id}
      allowsSorting
      isRowHeader={isRowHeader}
      className={align === 'right' ? 'text-right' : undefined}
    >
      {({ sortDirection }: { sortDirection?: 'ascending' | 'descending' }) => (
        <Table.SortableColumnHeader
          sortDirection={sortDirection}
          className={align === 'right' ? 'justify-end gap-1' : undefined}
        >
          {label}
        </Table.SortableColumnHeader>
      )}
    </Table.Column>
  );
}

/**
 * Name cell that switches to a text input on click, letting the user rename a
 * line item in place. Saves on Enter or blur (when changed) and reverts on Escape.
 */
function EditableNameCell({
  item,
  onSave
}: {
  item: LineItem;
  onSave(item: LineItem, patch: { name: string }): Promise<void>;
}) {
  const [editing, setEditing] = useState(false);
  const [value, setValue] = useState(item.name);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    if (!editing) {
      setValue(item.name);
    }
  }, [item.name, editing]);

  const cancel = () => {
    setValue(item.name);
    setEditing(false);
  };

  const commit = async () => {
    const trimmed = value.trim();
    if (!trimmed || trimmed === item.name) {
      cancel();
      return;
    }
    setSaving(true);
    try {
      await onSave(item, { name: trimmed });
      setEditing(false);
    } catch {
      setValue(item.name);
    } finally {
      setSaving(false);
    }
  };

  if (!editing) {
    return (
      <button
        type="button"
        onClick={() => setEditing(true)}
        aria-label={`Edit ${item.name} name`}
        className="-mx-1 max-w-full truncate rounded px-1 text-left font-medium transition-colors hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
      >
        {item.name}
      </button>
    );
  }

  return (
    <input
      type="text"
      autoFocus
      value={value}
      disabled={saving}
      aria-label="Name"
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

/**
 * Category cell that switches to an inline {@link CategorySelect} on click. The
 * picker opens immediately; selecting a different category saves it, and closing
 * the picker returns to the read-only display.
 */
function EditableCategoryCell({
  item,
  kind,
  categoriesById,
  onSave
}: {
  item: LineItem;
  kind: 'income' | 'expense';
  categoriesById: Map<number, Category>;
  onSave(item: LineItem, patch: { categoryId: number }): Promise<void>;
}) {
  const [editing, setEditing] = useState(false);
  const category = categoriesById.get(item.categoryId);

  if (!editing) {
    return (
      <button
        type="button"
        onClick={() => setEditing(true)}
        aria-label={`Edit ${item.name} category`}
        className="-mx-1 flex max-w-full items-center gap-1.5 rounded px-1 text-left transition-colors hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
      >
        {category?.icon && <span aria-hidden="true">{category.icon}</span>}
        <span className="truncate">{category?.name || '—'}</span>
      </button>
    );
  }

  return (
    <CategorySelect
      aria-label="Category"
      value={item.categoryId}
      kind={kind === 'income' ? 'Income' : 'Expense'}
      defaultOpen
      onChange={categoryId => {
        if (categoryId !== item.categoryId) {
          void onSave(item, { categoryId });
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

function DetailsModal({
  item,
  currency,
  tagNames,
  onClose,
  onTagClick
}: {
  item: LineItem | undefined;
  currency: string;
  tagNames: string[];
  onClose(): void;
  onTagClick(name: string): void;
}) {
  return (
    <Modal isOpen={item !== undefined} onOpenChange={open => !open && onClose()}>
      <Modal.Backdrop>
        <Modal.Container size="sm">
          <Modal.Dialog>
            <Modal.CloseTrigger />
            <Modal.Header>
              <Modal.Heading>{item?.name}</Modal.Heading>
            </Modal.Header>
            <Modal.Body className="space-y-4 text-sm">
              {item && (
                <>
                  {item.description ? (
                    <p className="text-muted">{item.description}</p>
                  ) : (
                    <p className="text-muted italic">No description.</p>
                  )}
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-wide text-muted mb-1">Tags</p>
                    {tagNames.length === 0 ? (
                      <span className="text-xs text-muted">None</span>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {tagNames.map(name => (
                          <button key={name} type="button" onClick={() => onTagClick(name)} className="no-underline">
                            <Chip variant="soft" size="sm">
                              {name}
                            </Chip>
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                  <div>
                    <p className="text-xs font-semibold uppercase tracking-wide text-muted mb-1">Amount</p>
                    <p>
                      {formatMoney(item.amount, currency)} per {cadenceLabel(item.cadence).toLowerCase()} period
                    </p>
                  </div>
                </>
              )}
            </Modal.Body>
            <Modal.Footer>
              <Button slot="close" variant="outline">
                Close
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

function TableFooter({ table }: { table: TableApi }) {
  return (
    <div className="flex flex-col gap-3 px-3 pt-4 sm:flex-row sm:items-center sm:justify-end">
      <div className="flex items-center gap-2">
        <span className="text-sm text-muted">Rows per page:</span>
        <SelectField
          aria-label="Rows per page"
          value={String(table.pageSize)}
          onChange={value => table.setPageSize(Number(value))}
          options={PAGE_SIZE_OPTIONS}
          className="w-20"
        />
      </div>
      <div className="flex items-center gap-3">
        <span className="text-sm text-muted tabular-nums">
          {table.rangeStart}-{table.rangeEnd} of {table.filteredCount}
        </span>
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            aria-label="Previous page"
            isDisabled={table.page === 0}
            onPress={() => table.setPage(table.page - 1)}
          >
            ‹
          </Button>
          <Button
            variant="ghost"
            size="sm"
            aria-label="Next page"
            isDisabled={table.page >= table.pageCount - 1}
            onPress={() => table.setPage(table.page + 1)}
          >
            ›
          </Button>
        </div>
      </div>
    </div>
  );
}

interface LineItemsTableProps {
  title: string;
  kind: 'income' | 'expense';
  isIncome: boolean;
  currency: string;
  items: LineItem[];
  searchTerm: string;
  sortKey: string;
  sortDirection: 'asc' | 'desc';
  onSortChange(key: string, direction: 'asc' | 'desc'): void;
  categoriesById: Map<number, Category>;
  tagsById: Map<number, Tag>;
  onDetails(item: LineItem): void;
  onEdit(item: LineItem): void;
  onDelete(item: LineItem): void;
  onInlineSave(item: LineItem, patch: { name?: string; categoryId?: number }): Promise<void>;
}

/**
 * Renders the income/expense line items in a sortable HeroUI table. Memoized so
 * that unrelated parent state changes (e.g. opening an edit/details modal) don't
 * force a costly rebuild of the react-aria table collection. Only re-renders when
 * its data, search term, sort, or stable callbacks change.
 */
const LineItemsTable = memo(function LineItemsTable({
  title,
  kind,
  isIncome,
  currency,
  items,
  searchTerm,
  sortKey,
  sortDirection,
  onSortChange,
  categoriesById,
  tagsById,
  onDetails,
  onEdit,
  onDelete,
  onInlineSave
}: LineItemsTableProps) {
  const sortDescriptor: SortDescriptor = {
    column: sortKey,
    direction: sortDirection === 'asc' ? 'ascending' : 'descending'
  };

  const handleSortChange = (descriptor: SortDescriptor) => {
    onSortChange(String(descriptor.column), descriptor.direction === 'descending' ? 'desc' : 'asc');
  };

  const renderCells = (item: LineItem) => {
    const breakdown = cadenceBreakdown(item.amount, item.cadence);
    const tagNames = item.tagIds.map(id => tagsById.get(id)?.name ?? '').filter(name => name !== '');

    const cells = [
      <Table.Cell key="details">
        <Button
          isIconOnly
          variant="ghost"
          size="sm"
          aria-label={`View ${item.name} details`}
          onPress={() => onDetails(item)}
        >
          <Eye className="size-4" />
        </Button>
      </Table.Cell>,
      <Table.Cell key="name">
        <EditableNameCell item={item} onSave={onInlineSave} />
      </Table.Cell>,
      <Table.Cell key="category">
        <EditableCategoryCell item={item} kind={kind} categoriesById={categoriesById} onSave={onInlineSave} />
      </Table.Cell>,
      <Table.Cell key="tags">
        {tagNames.length === 0 ? (
          <span className="text-muted">—</span>
        ) : (
          <span className="flex flex-wrap gap-1">
            {tagNames.map(name => (
              <Chip key={name} variant="soft" size="sm">
                {name}
              </Chip>
            ))}
          </span>
        )}
      </Table.Cell>
    ];

    if (isIncome) {
      cells.push(
        <Table.Cell key="amount" className="text-right font-semibold tabular-nums text-success">
          {formatMoney(item.amount, currency)}
        </Table.Cell>
      );
    } else {
      cells.push(
        <Table.Cell key="cadence">
          <Chip variant="soft" size="sm">
            {cadenceLabel(item.cadence)}
          </Chip>
        </Table.Cell>,
        ...COST_COLUMNS.map(col => (
          <Table.Cell key={col.key} className="text-right tabular-nums">
            {formatMoney(breakdown[col.key], currency)}
          </Table.Cell>
        ))
      );
    }

    cells.push(
      <Table.Cell key="actions">
        <div className="flex items-center justify-end gap-1">
          <Button isIconOnly variant="ghost" size="sm" onPress={() => onEdit(item)} aria-label={`Edit ${item.name}`}>
            <Pencil className="size-4" />
          </Button>
          <Button
            isIconOnly
            variant="danger-soft"
            size="sm"
            onPress={() => onDelete(item)}
            aria-label={`Delete ${item.name}`}
          >
            <Trash2 className="size-4" />
          </Button>
        </div>
      </Table.Cell>
    );

    return cells;
  };

  return (
    <Table aria-label={`${title} line items`}>
      <Table.ScrollContainer>
        <Table.Content
          aria-label={`${title} line items`}
          sortDescriptor={sortDescriptor}
          onSortChange={handleSortChange}
          className="min-w-[40rem]"
        >
          <Table.Header>
            <Table.Column id="details" width={48} aria-label="Details">
              {''}
            </Table.Column>
            <SortableColumn id="name" label="Name" isRowHeader />
            <SortableColumn id="category" label="Category" />
            <Table.Column id="tags">Tags</Table.Column>
            {isIncome ? (
              <SortableColumn id="amount" label="Amount" align="right" />
            ) : (
              <>
                <SortableColumn id="cadence" label="Frequency" />
                {COST_COLUMNS.map(col => (
                  <SortableColumn key={col.key} id={col.key} label={col.label} align="right" />
                ))}
              </>
            )}
            <Table.Column id="actions" className="text-right">
              Actions
            </Table.Column>
          </Table.Header>
          <Table.Body
            items={items}
            renderEmptyState={() => (
              <span className="block py-8 text-center text-sm text-muted">
                No {kind} matches “{searchTerm}”.
              </span>
            )}
          >
            {(item: LineItem) => <Table.Row id={item.id}>{renderCells(item)}</Table.Row>}
          </Table.Body>
        </Table.Content>
      </Table.ScrollContainer>
    </Table>
  );
});
