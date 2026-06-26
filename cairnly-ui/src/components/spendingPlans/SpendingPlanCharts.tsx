import { useEffect, useMemo } from 'react';
import { Bar, BarChart, Cell, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card, CardContent, CardHeader, Spinner } from '@heroui/react';
import { useSpendingPlanExpenses, useSpendingPlanIncomes } from '../../hooks/spendingPlans';
import { cadenceBreakdown } from '../../utils/cadence';
import { formatMoney, minorToMajor } from '../../utils/money';
import type { SpendingPlanExpense, SpendingPlanIncome, SpendingPlanSummary } from '../../types/spendingPlans';

/** Categorical palette for composition slices (works on dark + light). */
const PALETTE = [
  'var(--accent)',
  '#38bdf8',
  '#a78bfa',
  '#fbbf24',
  '#fb7185',
  '#34d399',
  '#f472b6',
  '#60a5fa'
];
const OTHER_COLOR = '#94a3b8';

/** A single composition slice (monthly amount in minor units). */
interface Slice {
  name: string;
  value: number;
  color: string;
}

interface SpendingPlanChartsProps {
  spendingPlanId: number;
  currency: string;
  summary: SpendingPlanSummary | undefined;
  summaryLoading: boolean;
}

/**
 * Builds a top-N (+ "Other") monthly composition from line items, normalized to
 * monthly minor units and sorted by size.
 */
function buildComposition(items: ReadonlyArray<SpendingPlanIncome | SpendingPlanExpense>): Slice[] {
  const monthly = items
    .map(item => ({ name: item.name, value: Math.round(cadenceBreakdown(item.amount, item.cadence).monthly) }))
    .filter(slice => slice.value > 0)
    .sort((a, b) => b.value - a.value);

  const TOP = 6;
  const top = monthly.slice(0, TOP);
  const rest = monthly.slice(TOP);

  const slices: Slice[] = top.map((slice, index) => ({ ...slice, color: PALETTE[index % PALETTE.length] }));

  if (rest.length > 0) {
    slices.push({
      name: `Other (${rest.length})`,
      value: rest.reduce((sum, slice) => sum + slice.value, 0),
      color: OTHER_COLOR
    });
  }

  return slices;
}

/** Compact axis label for minor-unit values (e.g. 1_250_000 cents -> "$13k"). */
function compactMoney(minorUnits: number, currency: string): string {
  const major = minorToMajor(minorUnits, currency);
  const abs = Math.abs(major);
  const sign = major < 0 ? '-' : '';

  if (abs >= 1000) {
    return `${sign}$${Math.round(abs / 1000)}k`;
  }

  return `${sign}$${Math.round(abs)}`;
}

/**
 * Visualizes a spending plan: monthly income vs expenses vs remaining, plus
 * income-source and expense-breakdown composition donuts. All figures are
 * normalized to a monthly cadence. Line items are loaded client-side (the set is
 * small).
 */
export function SpendingPlanCharts({ spendingPlanId, currency, summary, summaryLoading }: SpendingPlanChartsProps) {
  const incomesQuery = useSpendingPlanIncomes(spendingPlanId);
  const expensesQuery = useSpendingPlanExpenses(spendingPlanId);

  // Eagerly load every page so the composition reflects all line items.
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

  const incomes = useMemo(
    () => incomesQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [],
    [incomesQuery.data]
  );
  const expenses = useMemo(
    () => expensesQuery.data?.pages.flatMap(page => page.nodes ?? []) ?? [],
    [expensesQuery.data]
  );

  const incomeSlices = useMemo(() => buildComposition(incomes), [incomes]);
  const expenseSlices = useMemo(() => buildComposition(expenses), [expenses]);

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

  const isLoading = incomesQuery.isLoading || expensesQuery.isLoading || summaryLoading;

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
                  formatter={(value) => [formatMoney(Number(value), currency), '']}
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

      <DonutCard title="Income sources" slices={incomeSlices} currency={currency} emptyText="No income to chart yet." />
      <DonutCard title="Expense breakdown" slices={expenseSlices} currency={currency} emptyText="No expenses to chart yet." />
    </div>
  );
}

/** A composition donut with a centered monthly total and a labeled legend. */
function DonutCard({
  title,
  slices,
  currency,
  emptyText
}: {
  title: string;
  slices: Slice[];
  currency: string;
  emptyText: string;
}) {
  const total = slices.reduce((sum, slice) => sum + slice.value, 0);

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6">
        <h2 className="text-lg font-semibold">{title}</h2>
        <p className="text-sm text-muted">Normalized to a monthly cadence.</p>
      </CardHeader>
      <CardContent className="px-6 pb-6">
        {slices.length === 0 || total === 0 ? (
          <p className="py-10 text-center text-sm text-muted">{emptyText}</p>
        ) : (
          <>
            <div className="relative h-48">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={slices}
                    dataKey="value"
                    nameKey="name"
                    innerRadius={58}
                    outerRadius={80}
                    paddingAngle={2}
                    stroke="none"
                  >
                    {slices.map(slice => (
                      <Cell key={slice.name} fill={slice.color} />
                    ))}
                  </Pie>
                  <Tooltip content={<DonutTooltip currency={currency} total={total} />} />
                </PieChart>
              </ResponsiveContainer>
              <div className="pointer-events-none absolute inset-0 flex flex-col items-center justify-center">
                <span className="text-xs text-muted">Monthly</span>
                <span className="text-lg font-bold tabular-nums">{formatMoney(total, currency)}</span>
              </div>
            </div>

            <ul className="mt-4 space-y-1.5">
              {slices.map(slice => (
                <li key={slice.name} className="flex items-center gap-2 text-sm">
                  <span className="size-2.5 shrink-0 rounded-full" style={{ backgroundColor: slice.color }} aria-hidden="true" />
                  <span className="min-w-0 flex-1 truncate text-foreground">{slice.name}</span>
                  <span className="shrink-0 tabular-nums text-muted">{Math.round((slice.value / total) * 100)}%</span>
                  <span className="w-24 shrink-0 text-right tabular-nums text-foreground">
                    {formatMoney(slice.value, currency)}
                  </span>
                </li>
              ))}
            </ul>
          </>
        )}
      </CardContent>
    </Card>
  );
}

interface DonutTooltipPayloadItem {
  name: string;
  value: number;
}

/** Tooltip showing a slice's monthly amount and share of the total. */
function DonutTooltip({
  active,
  payload,
  currency,
  total
}: {
  active?: boolean;
  payload?: DonutTooltipPayloadItem[];
  currency: string;
  total: number;
}) {
  if (!active || !payload?.length) {
    return null;
  }

  const slice = payload[0];
  const pct = total > 0 ? Math.round((slice.value / total) * 100) : 0;

  return (
    <div className="rounded-lg border border-border bg-overlay px-3 py-2 text-sm shadow-lg">
      <p className="font-medium text-foreground">{slice.name}</p>
      <p className="text-muted">
        <span className="tabular-nums text-foreground">{formatMoney(slice.value, currency)}</span> · {pct}%
      </p>
    </div>
  );
}
