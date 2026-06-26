import { useMemo, useState } from 'react';
import { Link, useParams } from 'react-router';
import { Spinner } from '@heroui/react';
import { SpendingPlanSummaryCard } from '../components/spendingPlans/SpendingPlanSummaryCard';
import { SpendingPlanCharts } from '../components/spendingPlans/SpendingPlanCharts';
import { LineItemsSection } from '../components/spendingPlans/LineItemsSection';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { usePageHeader } from '../hooks/usePageHeader';
import { showErrorDetails } from '../utils/environment';
import { useSpendingPlan, useSpendingPlanSummary } from '../hooks/spendingPlans';

type DetailTab = 'overview' | 'charts';

const tabs: ReadonlyArray<{ id: DetailTab; label: string }> = [
  { id: 'overview', label: 'Overview' },
  { id: 'charts', label: 'Charts' }
];

export function SpendingPlanDetailPage() {
  const params = useParams<{ spendingPlanId: string }>();
  const spendingPlanId = params.spendingPlanId ? Number(params.spendingPlanId) : undefined;
  const validId = typeof spendingPlanId === 'number' && Number.isFinite(spendingPlanId) ? spendingPlanId : undefined;

  const [tab, setTab] = useState<DetailTab>('overview');

  const spendingPlanQuery = useSpendingPlan(validId);
  const summaryQuery = useSpendingPlanSummary(validId);

  const spendingPlanName = spendingPlanQuery.data?.name;
  const currency = spendingPlanQuery.data?.currency;

  const header = useMemo(
    () => ({
      title: (
        <div className="flex items-center gap-5">
          <span className="flex items-center gap-2">
            <Link
              to="/spending-plans"
              aria-label="Back to spending plans"
              className="text-muted no-underline transition-colors hover:text-foreground"
            >
              ←
            </Link>
            {spendingPlanName ?? 'Spending plan'}
          </span>
          <nav className="flex items-center gap-4 text-sm font-medium">
            {tabs.map(({ id, label }) => (
              <button
                key={id}
                type="button"
                onClick={() => setTab(id)}
                className={[
                  'border-b-2 pb-0.5 transition-colors',
                  tab === id ? 'border-accent text-accent' : 'border-transparent text-muted hover:text-foreground'
                ].join(' ')}
              >
                {label}
              </button>
            ))}
          </nav>
        </div>
      ),
      actions: currency ? (
        <span className="text-sm font-semibold uppercase tracking-wide text-muted">{currency}</span>
      ) : undefined
    }),
    [spendingPlanName, currency, tab]
  );

  usePageHeader(header);

  if (validId === undefined) {
    return <p className="text-center text-sm text-muted">Invalid spending plan.</p>;
  }

  if (spendingPlanQuery.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" color="accent" />
      </div>
    );
  }

  if (spendingPlanQuery.isError || !spendingPlanQuery.data) {
    return (
      <div className="space-y-4">
        <Link to="/spending-plans" className="text-sm font-medium text-accent no-underline">
          ← Back to spending plans
        </Link>
        <ApiErrorDisplay
          error={(spendingPlanQuery.error as Error) ?? new Error('Spending plan not found.')}
          title="Failed to load spending plan"
          showDetails={showErrorDetails}
        />
      </div>
    );
  }

  const spendingPlan = spendingPlanQuery.data;

  return (
    <div className="space-y-8">
      {tab === 'overview' ? (
        <>
          <SpendingPlanSummaryCard
            summary={summaryQuery.data}
            currency={spendingPlan.currency}
            isLoading={summaryQuery.isLoading}
          />

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 items-start">
            <LineItemsSection kind="income" spendingPlanId={spendingPlan.id} currency={spendingPlan.currency} />
            <LineItemsSection kind="expense" spendingPlanId={spendingPlan.id} currency={spendingPlan.currency} />
          </div>
        </>
      ) : (
        <SpendingPlanCharts
          spendingPlanId={spendingPlan.id}
          currency={spendingPlan.currency}
          summary={summaryQuery.data}
          summaryLoading={summaryQuery.isLoading}
        />
      )}
    </div>
  );
}
