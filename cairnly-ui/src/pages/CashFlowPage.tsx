import { useMemo, useState } from 'react';
import { useNavigate } from 'react-router';
import { Button, Card, CardContent, Spinner } from '@heroui/react';
import { SelectField } from '../components/SelectField';
import { TrendingUp } from 'lucide-react';
import { CashFlowChart } from '../components/cashflow/CashFlowChart';
import { CategoryBreakdown } from '../components/cashflow/CategoryBreakdown';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { EmptyState } from '../components/EmptyState';
import { usePageHeader } from '../hooks/usePageHeader';
import { useCashFlowReport } from '../hooks/reports';
import { showErrorDetails } from '../utils/environment';
import { aggregatePoint, summaryFor } from '../utils/cashflow';
import { formatMoney } from '../utils/money';
import { routePaths } from '../constants/routes';
import { balanceHistoryTimeframes } from '../types/accounts';
import { cashFlowPeriods } from '../types/reports';
import type { BalanceHistoryTimeframe } from '../types/accounts';
import type { CashFlowBreakdownItem, CashFlowDimension, CashFlowPeriod } from '../types/reports';

const viewOptions = [
  { value: 'bar', label: 'Bar Chart' },
  { value: 'line', label: 'Line Chart' }
];

interface StatTileProps {
  value: string;
  label: string;
  tone?: 'income' | 'expense' | 'neutral';
}

/** A centered headline figure with a caption, used for the cash-flow summary row. */
function StatTile({ value, label, tone = 'neutral' }: StatTileProps) {
  const valueClass = tone === 'income' ? 'text-success' : tone === 'expense' ? 'text-danger' : 'text-foreground';

  return (
    <Card className="bg-surface border border-border">
      <CardContent className="p-5 text-center">
        <p className={`text-2xl font-bold tabular-nums ${valueClass}`}>{value}</p>
        <p className="mt-1 text-xs font-semibold uppercase tracking-widest text-muted">{label}</p>
      </CardContent>
    </Card>
  );
}

/**
 * The Cash Flow page: a period income/expense/net chart, a summary row, and
 * income/expense breakdowns, backed by the reports API. Click a chart bucket to
 * drill into one period, and a breakdown bar to drill into its transactions.
 */
export function CashFlowPage() {
  const [timeframe, setTimeframe] = useState<BalanceHistoryTimeframe>('OneYear');
  const [period, setPeriod] = useState<CashFlowPeriod>('Monthly');
  const [view, setView] = useState('bar');
  const [selectedStart, setSelectedStart] = useState<string | null>(null);
  const navigate = useNavigate();

  const report = useCashFlowReport(timeframe, period);
  const currency = report.data?.currency ?? 'USD';
  const series = useMemo(() => report.data?.series ?? [], [report.data]);

  const selectedPoint = useMemo(() => series.find(p => p.periodStart === selectedStart), [series, selectedStart]);

  const active = useMemo(() => {
    if (selectedPoint) {
      return {
        income: selectedPoint.income,
        expenses: selectedPoint.expenses,
        incomeBreakdowns: selectedPoint.incomeBreakdowns,
        expenseBreakdowns: selectedPoint.expenseBreakdowns
      };
    }
    return aggregatePoint(series);
  }, [selectedPoint, series]);

  const summary = useMemo(() => summaryFor(active.income, active.expenses), [active]);

  const header = useMemo(
    () => ({
      title: 'Cash Flow',
      actions: (
        <div className="hidden rounded-lg border border-border p-0.5 text-sm sm:flex">
          {cashFlowPeriods.map(option => (
            <button
              key={option.value}
              type="button"
              onClick={() => setPeriod(option.value)}
              className={[
                'rounded-md px-3 py-1 transition-colors',
                period === option.value ? 'bg-surface-secondary text-foreground' : 'text-muted hover:text-foreground'
              ].join(' ')}
            >
              {option.label}
            </button>
          ))}
        </div>
      )
    }),
    [period]
  );

  usePageHeader(header);

  const handleDrill = (section: 'income' | 'expense', dimension: CashFlowDimension, item: CashFlowBreakdownItem) => {
    const sp = new URLSearchParams({ section, label: item.label, timeframe, period });
    if (selectedStart) {
      sp.set('from', selectedStart);
    }
    navigate(`${routePaths.cashFlow}/${dimension}/${encodeURIComponent(item.key)}?${sp.toString()}`);
  };

  if (report.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner color="accent" />
      </div>
    );
  }

  if (report.isError) {
    return <ApiErrorDisplay error={report.error} showDetails={showErrorDetails} />;
  }

  if (series.length === 0) {
    return (
      <EmptyState
        icon={<TrendingUp className="size-6" />}
        title="No cash flow yet"
        subtitle="Add transactions to see your income and spending trends."
      />
    );
  }

  return (
    <div className="space-y-6">
      <Card className="bg-surface border border-border">
        <CardContent className="p-4 sm:p-6">
          <CashFlowChart
            data={series}
            view={view as 'bar' | 'line'}
            currency={currency}
            selectedStart={selectedStart}
            onSelectPeriod={setSelectedStart}
          />
        </CardContent>
      </Card>

      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <h2 className="text-lg font-semibold">{selectedPoint ? selectedPoint.label : 'All periods'}</h2>
          {selectedPoint && (
            <Button variant="ghost" size="sm" onClick={() => setSelectedStart(null)}>
              Clear
            </Button>
          )}
        </div>
        <div className="flex items-center gap-2 text-sm text-muted">
          <div className="w-32">
            <SelectField
              aria-label="Timeframe"
              value={timeframe}
              onChange={value => setTimeframe(value as BalanceHistoryTimeframe)}
              options={balanceHistoryTimeframes.map(t => ({ value: t.value, label: t.label }))}
            />
          </div>
          <div className="w-36">
            <SelectField aria-label="Chart view" value={view} onChange={setView} options={viewOptions} />
          </div>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatTile value={formatMoney(summary.income, currency)} label="Income" tone="income" />
        <StatTile value={formatMoney(summary.expenses, currency)} label="Expenses" tone="expense" />
        <StatTile
          value={formatMoney(summary.savings, currency)}
          label="Total Savings"
          tone={summary.savings < 0 ? 'expense' : 'income'}
        />
        <StatTile value={`${Math.round(summary.savingsRate)}%`} label="Savings Rate" />
      </div>

      <CategoryBreakdown
        title="Income"
        breakdowns={active.incomeBreakdowns}
        currency={currency}
        tone="income"
        onDrill={(dimension, item) => handleDrill('income', dimension, item)}
      />
      <CategoryBreakdown
        title="Expenses"
        breakdowns={active.expenseBreakdowns}
        currency={currency}
        tone="expense"
        onDrill={(dimension, item) => handleDrill('expense', dimension, item)}
      />
    </div>
  );
}
