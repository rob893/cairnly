import { Link, useParams } from 'react-router';
import { Spinner } from '@heroui/react';
import { BudgetSummaryCard } from '../components/budgets/BudgetSummaryCard';
import { LineItemsSection } from '../components/budgets/LineItemsSection';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { showErrorDetails } from '../utils/environment';
import { useBudget, useBudgetSummary } from '../hooks/budgets';

export function BudgetDetailPage() {
  const params = useParams<{ budgetId: string }>();
  const budgetId = params.budgetId ? Number(params.budgetId) : undefined;
  const validId = typeof budgetId === 'number' && Number.isFinite(budgetId) ? budgetId : undefined;

  const budgetQuery = useBudget(validId);
  const summaryQuery = useBudgetSummary(validId);

  if (validId === undefined) {
    return <p className="text-center text-sm text-muted">Invalid budget.</p>;
  }

  if (budgetQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" color="accent" />
      </div>
    );
  }

  if (budgetQuery.isError || !budgetQuery.data) {
    return (
      <div className="space-y-4">
        <Link to="/budgets" className="text-sm font-medium text-accent no-underline">
          ← Back to budgets
        </Link>
        <ApiErrorDisplay
          error={(budgetQuery.error as Error) ?? new Error('Budget not found.')}
          title="Failed to load budget"
          showDetails={showErrorDetails}
        />
      </div>
    );
  }

  const budget = budgetQuery.data;

  return (
    <div className="space-y-8">
      <div>
        <Link to="/budgets" className="text-sm font-medium text-accent no-underline">
          ← Back to budgets
        </Link>
        <div className="mt-3 relative overflow-hidden rounded-2xl border border-border bg-surface-secondary/40 p-8 cairnly-aurora">
          <div className="flex items-start justify-between gap-4 flex-wrap">
            <div>
              <p className="text-sm font-semibold uppercase tracking-widest text-accent">Budget</p>
              <h1 className="mt-2 text-3xl font-bold tracking-tight">{budget.name}</h1>
              {budget.description && <p className="text-muted mt-1">{budget.description}</p>}
            </div>
            <span className="text-sm font-semibold uppercase tracking-wide text-muted">{budget.currency}</span>
          </div>
        </div>
      </div>

      <BudgetSummaryCard summary={summaryQuery.data} currency={budget.currency} isLoading={summaryQuery.isLoading} />

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 items-start">
        <LineItemsSection kind="income" budgetId={budget.id} currency={budget.currency} />
        <LineItemsSection kind="expense" budgetId={budget.id} currency={budget.currency} />
      </div>
    </div>
  );
}
