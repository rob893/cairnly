import { useCallback, useEffect, useMemo, useState } from 'react';
import { Card } from '@heroui/react';
import { LineItemFormModal } from './LineItemFormModal';
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
import { useCategories } from '../../hooks/categories';
import { buildSpendingPlanLineItemUpdateRequest } from '../../utils/patchRequest';
import type { CreateSpendingPlanExpenseRequest, CreateSpendingPlanIncomeRequest } from '../../types/spendingPlans';
import { DeleteLineItemModal } from './DeleteLineItemModal';
import { DetailsModal } from './DetailsModal';
import { LineItemsPanel } from './LineItemsPanel';
import type { LineItem, LineItemInlinePatch } from './lineItemTypes';
import { useLineItemsClientTable } from './useLineItemsClientTable';

interface LineItemsSectionProps {
  kind: 'income' | 'expense';
  spendingPlanId: number;
  currency: string;
}

/**
 * Orchestrates a spending plan's income or expense line-item table, including data loading,
 * local search/sort/pagination, inline edits, and create/edit/delete/details modals.
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

  useEffect(() => {
    if (query.hasNextPage && !query.isFetchingNextPage) {
      void query.fetchNextPage();
    }
  }, [query.hasNextPage, query.isFetchingNextPage, query]);

  const items = useMemo<LineItem[]>(
    () => (query.data?.pages.flatMap(page => page.nodes ?? []) ?? []) as LineItem[],
    [query.data]
  );
  const { table, tagNamesOf } = useLineItemsClientTable(items, categoriesById, tagsById);

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

  function openCreate(): void {
    setEditing(undefined);
    setFormOpen(true);
  }

  const openEdit = useCallback((item: LineItem) => {
    setEditing(item);
    setFormOpen(true);
  }, []);
  const openDetails = useCallback((item: LineItem) => setDetailsTarget(item), []);
  const openDeletePrompt = useCallback((item: LineItem) => setDeleteTarget(item), []);
  const updateIncomeAsync = updateIncome.mutateAsync;
  const updateExpenseAsync = updateExpense.mutateAsync;
  const handleInlineSave = useCallback(
    async (item: LineItem, patch: LineItemInlinePatch) => {
      const request = buildSpendingPlanLineItemUpdateRequest(item, patch);
      if (isIncome) {
        await updateIncomeAsync({ incomeId: item.id, request });
      } else {
        await updateExpenseAsync({ expenseId: item.id, request });
      }
    },
    [isIncome, updateIncomeAsync, updateExpenseAsync]
  );

  async function handleSubmit(
    payload: CreateSpendingPlanIncomeRequest & CreateSpendingPlanExpenseRequest
  ): Promise<void> {
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
  }

  async function handleDelete(): Promise<void> {
    if (!deleteTarget) {
      return;
    }
    if (isIncome) {
      await deleteIncome.mutateAsync(deleteTarget.id);
    } else {
      await deleteExpense.mutateAsync(deleteTarget.id);
    }
    setDeleteTarget(undefined);
  }

  return (
    <Card className="bg-surface border border-border">
      <LineItemsPanel
        title={title}
        addLabel={addLabel}
        kind={kind}
        isIncome={isIncome}
        currency={currency}
        isLoading={query.isLoading}
        isError={query.isError}
        error={query.error as Error | null}
        items={items}
        table={table}
        categoriesById={categoriesById}
        tagsById={tagsById}
        onCreate={openCreate}
        onDetails={openDetails}
        onEdit={openEdit}
        onDelete={openDeletePrompt}
        onInlineSave={handleInlineSave}
      />
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
      <DeleteLineItemModal
        kind={kind}
        item={deleteTarget}
        error={deleteMutation.error as Error | null}
        isPending={deleteMutation.isPending}
        onClose={() => setDeleteTarget(undefined)}
        onDelete={() => void handleDelete()}
      />
    </Card>
  );
}
