import { useMemo, useState } from 'react';
import { Button, Card, CardContent } from '@heroui/react';
import { SelectField } from '../components/SelectField';
import { CashFlowChart } from '../components/cashflow/CashFlowChart';
import { CategoryBreakdown } from '../components/cashflow/CategoryBreakdown';
import { usePageHeader } from '../hooks/usePageHeader';
import { Filter } from 'lucide-react';
import { formatMoney } from '../utils/money';
import {
  mockCashFlowCurrency,
  mockCashFlowSeries,
  mockCashFlowSummary,
  mockExpenseCategories,
  mockIncomeCategories
} from '../constants/mockCashFlow';

type Period = 'monthly' | 'quarterly' | 'yearly';

const periods: ReadonlyArray<{ id: Period; label: string }> = [
  { id: 'monthly', label: 'Monthly' },
  { id: 'quarterly', label: 'Quarterly' },
  { id: 'yearly', label: 'Yearly' }
];

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
  const valueClass =
    tone === 'income' ? 'text-success' : tone === 'expense' ? 'text-danger' : 'text-foreground';

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
 * The Cash Flow page: a monthly income/expense/net chart, a summary row, and
 * income/expense category breakdowns. Currently backed by placeholder data.
 */
export function CashFlowPage() {
  const [period, setPeriod] = useState<Period>('monthly');
  const [view, setView] = useState('bar');
  const currency = mockCashFlowCurrency;

  const header = useMemo(
    () => ({
      title: 'Cash Flow',
      actions: (
        <div className="flex items-center gap-2">
          <div className="hidden rounded-lg border border-border p-0.5 text-sm sm:flex">
            {periods.map(option => (
              <button
                key={option.id}
                type="button"
                onClick={() => setPeriod(option.id)}
                className={[
                  'rounded-md px-3 py-1 transition-colors',
                  period === option.id ? 'bg-surface-secondary text-foreground' : 'text-muted hover:text-foreground'
                ].join(' ')}
              >
                {option.label}
              </button>
            ))}
          </div>
          <Button variant="outline" size="sm">
            <Filter className="size-4" />
            Filters
          </Button>
        </div>
      )
    }),
    [period]
  );

  usePageHeader(header);

  return (
    <div className="space-y-6">
      <Card className="bg-surface border border-border">
        <CardContent className="space-y-4 p-4 sm:p-6">
          <CashFlowChart data={mockCashFlowSeries} />
          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border pt-4">
            <h2 className="text-lg font-semibold">June 2026</h2>
            <div className="flex items-center gap-2 text-sm text-muted">
              <span>View</span>
              <div className="w-36">
                <SelectField aria-label="Chart view" value={view} onChange={setView} options={viewOptions} />
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4">
        <StatTile value={formatMoney(mockCashFlowSummary.income, currency)} label="Income" tone="income" />
        <StatTile value={formatMoney(mockCashFlowSummary.expenses, currency)} label="Expenses" tone="expense" />
        <StatTile
          value={formatMoney(mockCashFlowSummary.savings, currency)}
          label="Total Savings"
          tone={mockCashFlowSummary.savings < 0 ? 'expense' : 'income'}
        />
        <StatTile value={`${mockCashFlowSummary.savingsRate}%`} label="Savings Rate" />
      </div>

      <CategoryBreakdown title="Income" items={mockIncomeCategories} currency={currency} tone="income" />
      <CategoryBreakdown title="Expenses" items={mockExpenseCategories} currency={currency} tone="expense" />
    </div>
  );
}
