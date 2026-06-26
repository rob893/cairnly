import { useState } from 'react';
import { Link } from 'react-router';
import { Button, Card, CardContent, Modal, Spinner } from '@heroui/react';
import { SpendingPlanFormModal } from './SpendingPlanFormModal';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import { useSpendingPlans, useDeleteSpendingPlan, useUpdateSpendingPlan } from '../../hooks/spendingPlans';
import type { SpendingPlan, CreateSpendingPlanRequest } from '../../types/spendingPlans';

interface SpendingPlansSectionProps {
  /** Opens the create-spendingPlan flow (the trigger lives in the page top bar). */
  onCreate: () => void;
}

/** Lists the current user's spendingPlans as cards with edit and delete controls. */
export function SpendingPlansSection({ onCreate }: SpendingPlansSectionProps) {
  const spendingPlansQuery = useSpendingPlans();

  const spendingPlans: SpendingPlan[] = spendingPlansQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [];

  return (
    <div className="space-y-6">
      {spendingPlansQuery.isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner size="lg" color="accent" />
        </div>
      ) : spendingPlansQuery.isError ? (
        <ApiErrorDisplay error={spendingPlansQuery.error as Error} title="Failed to load spending plans" showDetails={showErrorDetails} />
      ) : spendingPlans.length === 0 ? (
        <Card className="bg-surface border border-border">
          <CardContent className="p-10 text-center space-y-3">
            <p className="text-lg font-semibold">No spending plans yet</p>
            <p className="text-sm text-muted">Create your first spending plan to start planning income and expenses.</p>
            <div className="pt-2">
              <Button onPress={onCreate}>New spending plan</Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {spendingPlans.map(spendingPlan => (
            <SpendingPlanCard key={spendingPlan.id} spendingPlan={spendingPlan} />
          ))}
        </div>
      )}

      {spendingPlansQuery.hasNextPage && (
        <div className="flex justify-center">
          <Button
            variant="outline"
            onPress={() => spendingPlansQuery.fetchNextPage()}
            isPending={spendingPlansQuery.isFetchingNextPage}
          >
            Load more
          </Button>
        </div>
      )}
    </div>
  );
}

function SpendingPlanCard({ spendingPlan }: { spendingPlan: SpendingPlan }) {
  const updateSpendingPlan = useUpdateSpendingPlan(spendingPlan.id);
  const deleteSpendingPlan = useDeleteSpendingPlan();

  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const handleEdit = async (payload: CreateSpendingPlanRequest) => {
    await updateSpendingPlan.mutateAsync(payload);
    setEditOpen(false);
  };

  const handleDelete = async () => {
    await deleteSpendingPlan.mutateAsync(spendingPlan.id);
    setDeleteOpen(false);
  };

  return (
    <Card className="relative h-full bg-surface border border-border transition-colors hover:border-accent/50">
      {/* Full-card click target (stretched link); interactive controls sit above it via z-index. */}
      <Link
        to={`/spending-plans/${spendingPlan.id}`}
        aria-label={`Open ${spendingPlan.name}`}
        className="absolute inset-0 z-10 rounded-[inherit] outline-none focus-visible:ring-2 focus-visible:ring-focus focus-visible:ring-offset-2 focus-visible:ring-offset-surface"
      />
      <CardContent className="p-6 flex flex-col gap-3 h-full">
        <div className="flex items-start justify-between gap-2">
          <h2 className="text-lg font-semibold text-foreground">{spendingPlan.name}</h2>
          <span className="text-xs font-semibold uppercase tracking-wide text-muted">{spendingPlan.currency}</span>
        </div>
        {spendingPlan.description && <p className="text-sm text-muted leading-relaxed">{spendingPlan.description}</p>}

        <div className="pointer-events-none relative z-20 mt-auto flex items-center justify-end gap-2 pt-2">
          <Button
            variant="ghost"
            size="sm"
            className="pointer-events-auto"
            onPress={() => {
              updateSpendingPlan.reset();
              setEditOpen(true);
            }}
          >
            Edit
          </Button>
          <Button variant="danger-soft" size="sm" className="pointer-events-auto" onPress={() => setDeleteOpen(true)}>
            Delete
          </Button>
        </div>
      </CardContent>

      <SpendingPlanFormModal
        isOpen={editOpen}
        onOpenChange={setEditOpen}
        spendingPlan={spendingPlan}
        onSubmit={handleEdit}
        isPending={updateSpendingPlan.isPending}
        error={updateSpendingPlan.error as Error | null}
      />

      <Modal isOpen={deleteOpen} onOpenChange={setDeleteOpen}>
        <Modal.Backdrop>
          <Modal.Container size="sm">
            <Modal.Dialog>
              <Modal.CloseTrigger />
              <Modal.Header>
                <Modal.Heading>Delete spending plan?</Modal.Heading>
              </Modal.Header>
              <Modal.Body className="space-y-4">
                {deleteSpendingPlan.error && (
                  <ApiErrorDisplay error={deleteSpendingPlan.error as Error} title="Delete failed" showDetails={showErrorDetails} />
                )}
                <p className="text-sm text-muted">
                  Permanently delete <span className="font-medium text-foreground">{spendingPlan.name}</span> and all of its
                  income and expense line items? This cannot be undone.
                </p>
              </Modal.Body>
              <Modal.Footer>
                <Button slot="close" variant="outline">
                  Cancel
                </Button>
                <Button variant="danger" onPress={handleDelete} isPending={deleteSpendingPlan.isPending}>
                  Delete spending plan
                </Button>
              </Modal.Footer>
            </Modal.Dialog>
          </Modal.Container>
        </Modal.Backdrop>
      </Modal>
    </Card>
  );
}
