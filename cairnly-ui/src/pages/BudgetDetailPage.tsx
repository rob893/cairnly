import { useMemo } from 'react';
import { Link, useParams } from 'react-router';
import { Spinner } from '@heroui/react';
import { BudgetSummaryCard } from '../components/budgets/BudgetSummaryCard';
import { LineItemsSection } from '../components/budgets/LineItemsSection';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { usePageHeader } from '../hooks/usePageHeader';
import { showErrorDetails } from '../utils/environment';
import { useBudget, useBudgetSummary } from '../hooks/budgets';

export function BudgetDetailPage() {
  const params = useParams<{ budgetId: string }>();
  const budgetId = params.budgetId ? Number(params.budgetId) : undefined;
  const validId = typeof budgetId === 'number' && Number.isFinite(budgetId) ? budgetId : undefined;

  const budgetQuery = useBudget(validId);
  const summaryQuery = useBudgetSummary(validId);

  const budgetName = budgetQuery.data?.name;
  const currency = budgetQuery.data?.currency;

  const header = useMemo(
    () => ({
      title: (
        <span className="flex items-center gap-2">
          <Link
            to="/budgets"
            aria-label="Back to budgets"
            className="text-muted no-underline transition-colors hover:text-foreground"
          >
            ←
          </Link>
          {budgetName ?? 'Budget'}
        </span>
      ),
      actions: currency ? (
        <span className="text-sm font-semibold uppercase tracking-wide text-muted">{currency}</span>
      ) : undefined
    }),
    [budgetName, currency]
  );

  usePageHeader(header);

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
      <BudgetSummaryCard summary={summaryQuery.data} currency={budget.currency} isLoading={summaryQuery.isLoading} />

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 items-start">
        <LineItemsSection kind="income" budgetId={budget.id} currency={budget.currency} />
        <LineItemsSection kind="expense" budgetId={budget.id} currency={budget.currency} />
      </div>
    </div>
  );
}
