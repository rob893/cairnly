import { useEffect, useMemo, useState } from 'react';
import { Bar, BarChart, Cell, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card, CardContent, CardHeader, Spinner } from '@heroui/react';
import { useSpendingPlanExpenses, useSpendingPlanIncomes } from '../../hooks/spendingPlans';
import { useCategories } from '../../hooks/categories';
import type { SpendingPlanSummary } from '../../types/spendingPlans';
import { formatMoney } from '../../utils/money';
import { CashFlowSankeyCard } from './CashFlowSankeyCard';
import { DonutCard } from './DonutCard';
import { buildComposition, buildFlow, compactMoney, type DonutMode, type FlowMode } from './spendingPlanChartUtils';

interface SpendingPlanChartsProps {
  spendingPlanId: number;
  currency: string;
  summary: SpendingPlanSummary | undefined;
  summaryLoading: boolean;
}

/** Visualizes a spending plan's monthly cash flow, composition donuts, and Sankey flow. */
export function SpendingPlanCharts({ spendingPlanId, currency, summary, summaryLoading }: SpendingPlanChartsProps) {
  const incomesQuery = useSpendingPlanIncomes(spendingPlanId);
  const expensesQuery = useSpendingPlanExpenses(spendingPlanId);
  const { categoriesById, isLoading: categoriesLoading } = useCategories();
  const [flowMode, setFlowMode] = useState<FlowMode>('category-group');
  const [incomeMode, setIncomeMode] = useState<DonutMode>('source');
  const [expenseMode, setExpenseMode] = useState<DonutMode>('source');

  useEffect(() => {
    if (incomesQuery.hasNextPage && !incomesQuery.isFetchingNextPage) {
      void incomesQuery.fetchNextPage();
    }
  }, [incomesQuery.hasNextPage, incomesQuery.isFetchingNextPage, incomesQuery]);

  useEffect(() => {
    if (expensesQuery.hasNextPage && !expensesQuery.isFetchingNextPage) {
      void expensesQuery.fetchNextPage();
    }
  }, [expensesQuery.hasNextPage, expensesQuery.isFetchingNextPage, expensesQuery]);

  const incomes = useMemo(() => incomesQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [], [incomesQuery.data]);
  const expenses = useMemo(
    () => expensesQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [],
    [expensesQuery.data]
  );
  const incomeSlices = useMemo(
    () => buildComposition(incomes, categoriesById, incomeMode),
    [incomes, categoriesById, incomeMode]
  );
  const expenseSlices = useMemo(
    () => buildComposition(expenses, categoriesById, expenseMode),
    [expenses, categoriesById, expenseMode]
  );
  const flowData = useMemo(
    () => buildFlow(incomes, expenses, categoriesById, flowMode),
    [incomes, expenses, categoriesById, flowMode]
  );
  const cashFlow = useMemo(() => {
    if (!summary) {
      return [];
    }

    return [
      { label: 'Income', value: Math.round(summary.income.monthly), fill: 'var(--success)' },
      { label: 'Expenses', value: Math.round(summary.expenses.monthly), fill: 'var(--danger)' },
      { label: 'Remaining', value: Math.round(summary.remaining.monthly), fill: 'var(--accent)' }
    ];
  }, [summary]);
  const isLoading = incomesQuery.isLoading || expensesQuery.isLoading || summaryLoading || categoriesLoading;

  if (isLoading) {
    return (
      <Card className="bg-surface border border-border">
        <CardContent className="flex justify-center py-16">
          <Spinner size="lg" color="accent" />
        </CardContent>
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <CashFlowSankeyCard flowData={flowData} currency={currency} mode={flowMode} onModeChange={setFlowMode} />

      <div className="grid grid-cols-1 gap-6 lg:grid-cols-3">
        <Card className="bg-surface border border-border">
          <CardHeader className="px-6 pt-6">
            <h2 className="text-lg font-semibold">Monthly cash flow</h2>
            <p className="text-sm text-muted">Income, expenses, and what's left over.</p>
          </CardHeader>
          <CardContent className="px-2 pb-4 sm:px-4">
            <div className="h-64 w-full">
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={cashFlow} margin={{ top: 8, right: 8, left: 8, bottom: 0 }}>
                  <XAxis
                    dataKey="label"
                    tick={{ fill: 'var(--muted)', fontSize: 12 }}
                    tickLine={false}
                    axisLine={{ stroke: 'var(--border)' }}
                  />
                  <YAxis
                    tick={{ fill: 'var(--muted)', fontSize: 12 }}
                    tickLine={false}
                    axisLine={false}
                    width={48}
                    tickFormatter={(value: number) => compactMoney(value, currency)}
                  />
                  <Tooltip
                    cursor={{ fill: 'var(--surface-secondary)', opacity: 0.5 }}
                    formatter={value => [formatMoney(Number(value), currency), '']}
                    contentStyle={{
                      background: 'var(--overlay)',
                      border: '1px solid var(--border)',
                      borderRadius: 8,
                      color: 'var(--foreground)'
                    }}
                    labelStyle={{ color: 'var(--foreground)' }}
                  />
                  <Bar dataKey="value" radius={[4, 4, 0, 0]} maxBarSize={64}>
                    {cashFlow.map(entry => (
                      <Cell key={entry.label} fill={entry.fill} />
                    ))}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
            </div>
          </CardContent>
        </Card>

        <DonutCard
          title="Income sources"
          slices={incomeSlices}
          currency={currency}
          emptyText="No income to chart yet."
          mode={incomeMode}
          onModeChange={setIncomeMode}
          modeAriaLabel="Change income grouping"
        />
        <DonutCard
          title="Expense breakdown"
          slices={expenseSlices}
          currency={currency}
          emptyText="No expenses to chart yet."
          mode={expenseMode}
          onModeChange={setExpenseMode}
          modeAriaLabel="Change expense grouping"
        />
      </div>
    </div>
  );
}
