import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { Button, Card, CardContent, Spinner } from '@heroui/react';
import { useBudgets, useBudgetSummaries } from '../hooks/budgets';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { StatCard } from '../components/dashboard/StatCard';
import { SpendingChartCard, type SpendingDatum } from '../components/dashboard/SpendingChartCard';
import { BudgetsListCard, type BudgetListItem } from '../components/dashboard/BudgetsListCard';
import { TrendDownIcon, TrendUpIcon, WalletIcon } from '../components/icons/NavIcons';
import { formatMoney, minorToMajor } from '../utils/money';
import { showErrorDetails } from '../utils/environment';
import { dashboardCadenceLabel, type DashboardCadence } from '../utils/dashboard';
import type { Budget } from '../types/budgets';

export function HomePage() {
  const budgetsQuery = useBudgets();
  const navigate = useNavigate();
  const [cadence, setCadence] = useState<DashboardCadence>('monthly');

  const budgets: Budget[] = useMemo(
    () => budgetsQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [],
    [budgetsQuery.data]
  );

  const budgetIds = useMemo(() => budgets.map(b => b.id), [budgets]);
  const { results, isLoading: summariesLoading } = useBudgetSummaries(budgetIds);

  const primaryCurrency = budgets[0]?.currency ?? 'USD';

  const summaryByBudgetId = useMemo(() => {
    const map = new Map<number, (typeof results)[number]['summary']>();
    for (const result of results) {
      map.set(result.budgetId, result.summary);
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
      budgets.map(budget => {
        const summary = summaryByBudgetId.get(budget.id);
        return {
          name: budget.name,
          income: summary ? minorToMajor(summary.income[cadence], budget.currency) : 0,
          expenses: summary ? minorToMajor(summary.expenses[cadence], budget.currency) : 0
        };
      }),
    [budgets, summaryByBudgetId, cadence]
  );

  const listItems: BudgetListItem[] = useMemo(
    () =>
      budgets.map(budget => {
        const summary = summaryByBudgetId.get(budget.id);
        return {
          id: budget.id,
          name: budget.name,
          currency: budget.currency,
          income: summary?.income[cadence] ?? 0,
          expenses: summary?.expenses[cadence] ?? 0,
          remaining: summary?.remaining[cadence] ?? 0
        };
      }),
    [budgets, summaryByBudgetId, cadence]
  );

  const budgetCountHint = `Across ${budgets.length} ${budgets.length === 1 ? 'budget' : 'budgets'}`;

  if (budgetsQuery.isLoading) {
    return (
      <div className="flex justify-center py-24">
        <Spinner size="lg" color="accent" />
      </div>
    );
  }

  if (budgetsQuery.isError) {
    return (
      <ApiErrorDisplay
        error={budgetsQuery.error as Error}
        title="Failed to load your dashboard"
        showDetails={showErrorDetails}
      />
    );
  }

  if (budgets.length === 0) {
    return (
      <Card className="bg-surface border border-border">
        <CardContent className="space-y-3 p-10 text-center">
          <p className="text-lg font-semibold">No budgets yet</p>
          <p className="text-sm text-muted">
            Create your first budget to see income, expenses, and remaining at a glance.
          </p>
          <div className="pt-2">
            <Button onPress={() => navigate('/budgets')}>Create a budget</Button>
          </div>
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
        <StatCard
          label={`${dashboardCadenceLabel[cadence]} income`}
          value={formatMoney(totals.income, primaryCurrency)}
          hint={budgetCountHint}
          tone="income"
          icon={<TrendUpIcon className="size-5" />}
        />
        <StatCard
          label={`${dashboardCadenceLabel[cadence]} expenses`}
          value={formatMoney(totals.expenses, primaryCurrency)}
          hint={budgetCountHint}
          tone="expense"
          icon={<TrendDownIcon className="size-5" />}
        />
        <StatCard
          label={`${dashboardCadenceLabel[cadence]} remaining`}
          value={formatMoney(totals.remaining, primaryCurrency)}
          hint={totals.remaining < 0 ? 'Over budget' : 'Left to allocate'}
          tone="neutral"
          icon={<WalletIcon className="size-5" />}
        />
      </div>

      <div className="grid grid-cols-1 gap-6 xl:grid-cols-3">
        <div className="xl:col-span-2">
          <SpendingChartCard
            data={chartData}
            currency={primaryCurrency}
            cadence={cadence}
            onCadenceChange={setCadence}
          />
        </div>
        <div className="xl:col-span-1">
          <BudgetsListCard items={listItems} isLoading={summariesLoading} />
        </div>
      </div>
    </div>
  );
}
