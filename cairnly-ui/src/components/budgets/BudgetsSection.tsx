import { useState } from 'react';
import { Link } from 'react-router';
import { Button, Card, CardContent, Modal, Spinner } from '@heroui/react';
import { BudgetFormModal } from './BudgetFormModal';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import { useBudgets, useCreateBudget, useDeleteBudget, useUpdateBudget } from '../../hooks/budgets';
import type { Budget, CreateBudgetRequest } from '../../types/budgets';

/** Lists the current user's budgets as cards with create, edit, and delete controls. */
export function BudgetsSection() {
  const budgetsQuery = useBudgets();
  const createBudget = useCreateBudget();

  const [formOpen, setFormOpen] = useState(false);

  const budgets: Budget[] = budgetsQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [];

  const openCreate = () => {
    createBudget.reset();
    setFormOpen(true);
  };

  const handleCreate = async (payload: CreateBudgetRequest) => {
    await createBudget.mutateAsync(payload);
    setFormOpen(false);
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between gap-4">
        <p className="text-sm text-muted">
          {budgets.length === 1 ? '1 budget' : `${budgets.length} budgets`}
        </p>
        <Button onPress={openCreate}>New budget</Button>
      </div>

      {budgetsQuery.isLoading ? (
        <div className="flex justify-center py-16">
          <Spinner size="lg" color="accent" />
        </div>
      ) : budgetsQuery.isError ? (
        <ApiErrorDisplay error={budgetsQuery.error as Error} title="Failed to load budgets" showDetails={showErrorDetails} />
      ) : budgets.length === 0 ? (
        <Card className="bg-surface border border-border">
          <CardContent className="p-10 text-center space-y-3">
            <p className="text-lg font-semibold">No budgets yet</p>
            <p className="text-sm text-muted">Create your first budget to start planning income and expenses.</p>
            <div className="pt-2">
              <Button onPress={openCreate}>New budget</Button>
            </div>
          </CardContent>
        </Card>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
          {budgets.map(budget => (
            <BudgetCard key={budget.id} budget={budget} />
          ))}
        </div>
      )}

      {budgetsQuery.hasNextPage && (
        <div className="flex justify-center">
          <Button
            variant="outline"
            onPress={() => budgetsQuery.fetchNextPage()}
            isPending={budgetsQuery.isFetchingNextPage}
          >
            Load more
          </Button>
        </div>
      )}

      <BudgetFormModal
        isOpen={formOpen}
        onOpenChange={setFormOpen}
        onSubmit={handleCreate}
        isPending={createBudget.isPending}
        error={createBudget.error as Error | null}
      />
    </div>
  );
}

function BudgetCard({ budget }: { budget: Budget }) {
  const updateBudget = useUpdateBudget(budget.id);
  const deleteBudget = useDeleteBudget();

  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);

  const handleEdit = async (payload: CreateBudgetRequest) => {
    await updateBudget.mutateAsync(payload);
    setEditOpen(false);
  };

  const handleDelete = async () => {
    await deleteBudget.mutateAsync(budget.id);
    setDeleteOpen(false);
  };

  return (
    <Card className="h-full bg-surface border border-border transition-colors hover:border-accent/50">
      <CardContent className="p-6 flex flex-col gap-3 h-full">
        <Link to={`/budgets/${budget.id}`} className="no-underline">
          <div className="flex items-start justify-between gap-2">
            <h2 className="text-lg font-semibold text-foreground">{budget.name}</h2>
            <span className="text-xs font-semibold uppercase tracking-wide text-muted">{budget.currency}</span>
          </div>
          {budget.description && <p className="mt-1 text-sm text-muted leading-relaxed">{budget.description}</p>}
        </Link>

        <div className="mt-auto flex items-center justify-between gap-2 pt-2">
          <Link to={`/budgets/${budget.id}`} className="text-sm font-medium text-accent no-underline">
            Open →
          </Link>
          <div className="flex items-center gap-2">
            <Button
              variant="ghost"
              size="sm"
              onPress={() => {
                updateBudget.reset();
                setEditOpen(true);
              }}
            >
              Edit
            </Button>
            <Button variant="danger-soft" size="sm" onPress={() => setDeleteOpen(true)}>
              Delete
            </Button>
          </div>
        </div>
      </CardContent>

      <BudgetFormModal
        isOpen={editOpen}
        onOpenChange={setEditOpen}
        budget={budget}
        onSubmit={handleEdit}
        isPending={updateBudget.isPending}
        error={updateBudget.error as Error | null}
      />

      <Modal isOpen={deleteOpen} onOpenChange={setDeleteOpen}>
        <Modal.Backdrop>
          <Modal.Container size="sm">
            <Modal.Dialog>
              <Modal.CloseTrigger />
              <Modal.Header>
                <Modal.Heading>Delete budget?</Modal.Heading>
              </Modal.Header>
              <Modal.Body className="space-y-4">
                {deleteBudget.error && (
                  <ApiErrorDisplay error={deleteBudget.error as Error} title="Delete failed" showDetails={showErrorDetails} />
                )}
                <p className="text-sm text-muted">
                  Permanently delete <span className="font-medium text-foreground">{budget.name}</span> and all of its
                  income and expense line items? This cannot be undone.
                </p>
              </Modal.Body>
              <Modal.Footer>
                <Button slot="close" variant="outline">
                  Cancel
                </Button>
                <Button variant="danger" onPress={handleDelete} isPending={deleteBudget.isPending}>
                  Delete budget
                </Button>
              </Modal.Footer>
            </Modal.Dialog>
          </Modal.Container>
        </Modal.Backdrop>
      </Modal>
    </Card>
  );
}
