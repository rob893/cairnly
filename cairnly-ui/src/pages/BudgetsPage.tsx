import { useCallback, useMemo, useState } from 'react';
import { Button } from '@heroui/react';
import { BudgetsSection } from '../components/budgets/BudgetsSection';
import { BudgetFormModal } from '../components/budgets/BudgetFormModal';
import { usePageHeader } from '../hooks/usePageHeader';
import { useCreateBudget } from '../hooks/budgets';
import { PlusIcon } from '../components/icons/NavIcons';
import type { CreateBudgetRequest } from '../types/budgets';

export function BudgetsPage() {
  const createBudget = useCreateBudget();
  const [formOpen, setFormOpen] = useState(false);

  const openCreate = useCallback(() => {
    createBudget.reset();
    setFormOpen(true);
  }, [createBudget]);

  const handleCreate = async (payload: CreateBudgetRequest) => {
    await createBudget.mutateAsync(payload);
    setFormOpen(false);
  };

  const header = useMemo(
    () => ({
      title: 'Budgets',
      actions: (
        <Button size="sm" onPress={openCreate}>
          <PlusIcon className="size-4" />
          New budget
        </Button>
      )
    }),
    [openCreate]
  );

  usePageHeader(header);

  return (
    <>
      <BudgetsSection onCreate={openCreate} />

      <BudgetFormModal
        isOpen={formOpen}
        onOpenChange={setFormOpen}
        onSubmit={handleCreate}
        isPending={createBudget.isPending}
        error={createBudget.error as Error | null}
      />
    </>
  );
}
