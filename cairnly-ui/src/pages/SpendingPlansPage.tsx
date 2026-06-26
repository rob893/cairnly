import { useCallback, useMemo, useState } from 'react';
import { Button } from '@heroui/react';
import { SpendingPlansSection } from '../components/spendingPlans/SpendingPlansSection';
import { SpendingPlanFormModal } from '../components/spendingPlans/SpendingPlanFormModal';
import { usePageHeader } from '../hooks/usePageHeader';
import { useCreateSpendingPlan } from '../hooks/spendingPlans';
import { Plus } from 'lucide-react';
import type { CreateSpendingPlanRequest } from '../types/spendingPlans';

export function SpendingPlansPage() {
  const createSpendingPlan = useCreateSpendingPlan();
  const [formOpen, setFormOpen] = useState(false);

  const openCreate = useCallback(() => {
    createSpendingPlan.reset();
    setFormOpen(true);
  }, [createSpendingPlan]);

  const handleCreate = async (payload: CreateSpendingPlanRequest) => {
    await createSpendingPlan.mutateAsync(payload);
    setFormOpen(false);
  };

  const header = useMemo(
    () => ({
      title: 'Spending Plans',
      actions: (
        <Button size="sm" onPress={openCreate}>
          <Plus className="size-4" />
          New spending plan
        </Button>
      )
    }),
    [openCreate]
  );

  usePageHeader(header);

  return (
    <>
      <SpendingPlansSection onCreate={openCreate} />

      <SpendingPlanFormModal
        isOpen={formOpen}
        onOpenChange={setFormOpen}
        onSubmit={handleCreate}
        isPending={createSpendingPlan.isPending}
        error={createSpendingPlan.error as Error | null}
      />
    </>
  );
}
