import { lazy, Suspense, useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { Card, CardContent, Spinner } from '@heroui/react';
import { useSpendingPlans, useSpendingPlanSummaries } from '../hooks/spendingPlans';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { EmptyState } from '../components/EmptyState';
import { StatCard } from '../components/dashboard/StatCard';
import type { SpendingDatum } from '../components/dashboard/SpendingChartCard';
import { SpendingPlansListCard, type SpendingPlanListItem } from '../components/dashboard/SpendingPlansListCard';
import { Scale, TrendingDown, TrendingUp, Wallet } from 'lucide-react';
import { formatMoney, minorToMajor } from '../utils/money';
import { showErrorDetails } from '../utils/environment';
import { dashboardCadenceLabel, type DashboardCadence } from '../utils/dashboard';
import type { SpendingPlan } from '../types/spendingPlans';

const SpendingChartCard = lazy(() =>
  import('../components/dashboard/SpendingChartCard').then(m => ({ default: m.SpendingChartCard }))
);

function ChartCardFallback() {
  return (
    <Card className="bg-surface border border-border">
      <CardContent className="flex h-96 items-center justify-center">
        <Spinner size="lg" color="accent" />
      </CardContent>
    </Card>
  );
}

export function HomePage() {
  const spendingPlansQuery = useSpendingPlans();
  const navigate = useNavigate();
  const [cadence, setCadence] = useState<DashboardCadence>('monthly');

  const spendingPlans: SpendingPlan[] = useMemo(
    () => spendingPlansQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [],
    [spendingPlansQuery.data]
  );

  const spendingPlanIds = useMemo(() => spendingPlans.map(b => b.id), [spendingPlans]);
  const { results, isLoading: summariesLoading } = useSpendingPlanSummaries(spendingPlanIds);

  const primaryCurrency = spendingPlans[0]?.currency ?? 'USD';

  const summaryBySpendingPlanId = useMemo(() => {
    const map = new Map<number, (typeof results)[number]['summary']>();
    for (const result of results) {
      map.set(result.spendingPlanId, result.summary);
    }
    return map;
  }, [results]);

  const totals = useMemo(() => {
    let income = 0;
    let expenses = 0;
    let remaining = 0;

    for (const result of results) {
      if (!result.summary) {
        continue;
      }
      income += result.summary.income[cadence];
      expenses += result.summary.expenses[cadence];
      remaining += result.summary.remaining[cadence];
    }

    return { income, expenses, remaining };
  }, [results, cadence]);

  const chartData: SpendingDatum[] = useMemo(
    () =>
      spendingPlans.map(spendingPlan => {
        const summary = summaryBySpendingPlanId.get(spendingPlan.id);
        return {
          name: spendingPlan.name,
          income: summary ? minorToMajor(summary.income[cadence], spendingPlan.currency) : 0,
          expenses: summary ? minorToMajor(summary.expenses[cadence], spendingPlan.currency) : 0
        };
      }),
    [spendingPlans, summaryBySpendingPlanId, cadence]
  );

  const listItems: SpendingPlanListItem[] = useMemo(
    () =>
      spendingPlans.map(spendingPlan => {
        const summary = summaryBySpendingPlanId.get(spendingPlan.id);
        return {
          id: spendingPlan.id,
          name: spendingPlan.name,
          currency: spendingPlan.currency,
          income: summary?.income[cadence] ?? 0,
          expenses: summary?.expenses[cadence] ?? 0,
          remaining: summary?.remaining[cadence] ?? 0
        };
      }),
    [spendingPlans, summaryBySpendingPlanId, cadence]
  );

  const spendingPlanCountHint = `Across ${spendingPlans.length} ${spendingPlans.length === 1 ? 'spending plan' : 'spending plans'}`;

  if (spendingPlansQuery.isLoading) {
    return (
      <div className="flex justify-center py-24">
        <Spinner size="lg" color="accent" />
      </div>
    );
  }

  if (spendingPlansQuery.isError) {
    return (
      <ApiErrorDisplay
        error={spendingPlansQuery.error as Error}
        title="Failed to load your dashboard"
        showDetails={showErrorDetails}
      />
    );
  }

  if (spendingPlans.length === 0) {
    return (
      <EmptyState
        icon={<Scale className="size-5" />}
        title="No spending plans yet"
        subtitle="Create your first spending plan to see income, expenses, and remaining at a glance."
        cta={{ label: 'Create a spending plan', onPress: () => navigate('/spending-plans') }}
      />
    );
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatCard
          label={`${dashboardCadenceLabel[cadence]} income`}
          value={formatMoney(totals.income, primaryCurrency)}
          hint={spendingPlanCountHint}
          tone="income"
          icon={<TrendingUp className="size-5" />}
        />
        <StatCard
          label={`${dashboardCadenceLabel[cadence]} expenses`}
          value={formatMoney(totals.expenses, primaryCurrency)}
          hint={spendingPlanCountHint}
          tone="expense"
          icon={<TrendingDown className="size-5" />}
        />
        <StatCard
          label={`${dashboardCadenceLabel[cadence]} remaining`}
          value={formatMoney(totals.remaining, primaryCurrency)}
          hint={totals.remaining < 0 ? 'Overspent' : 'Left to allocate'}
          tone="neutral"
          icon={<Wallet className="size-5" />}
        />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <div className="xl:col-span-2">
          <Suspense fallback={<ChartCardFallback />}>
            <SpendingChartCard
              data={chartData}
              currency={primaryCurrency}
              cadence={cadence}
              onCadenceChange={setCadence}
            />
          </Suspense>
        </div>
        <div className="xl:col-span-1">
          <SpendingPlansListCard items={listItems} isLoading={summariesLoading} />
        </div>
      </div>
    </div>
  );
}
