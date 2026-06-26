import { useState } from 'react';
import { Card, CardContent, CardHeader } from '@heroui/react';
import { formatMoney } from '../../utils/money';
import type { MockCashFlowCategory } from '../../constants/mockCashFlow';

type GroupBy = 'category' | 'group' | 'merchant';

const groupTabs: ReadonlyArray<{ id: GroupBy; label: string }> = [
  { id: 'category', label: 'Category' },
  { id: 'group', label: 'Group' },
  { id: 'merchant', label: 'Merchant' }
];

interface CategoryBreakdownProps {
  title: string;
  items: MockCashFlowCategory[];
  currency: string;
  /** Bar tone: income (green) or expense (red). */
  tone: 'income' | 'expense';
}

/**
 * A section listing income or expense categories as proportional horizontal bars
 * with amounts and percentage shares, with stubbed grouping tabs.
 */
export function CategoryBreakdown({ title, items, currency, tone }: CategoryBreakdownProps) {
  const [groupBy, setGroupBy] = useState<GroupBy>('category');
  const barClass = tone === 'income' ? 'bg-success/25' : 'bg-danger/25';

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="flex flex-wrap items-center justify-between gap-3 px-6 pt-6">
        <h2 className="text-lg font-semibold">{title}</h2>
        <div className="flex rounded-lg border border-border p-0.5 text-sm">
          {groupTabs.map(tab => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setGroupBy(tab.id)}
              className={[
                'rounded-md px-3 py-1 transition-colors',
                groupBy === tab.id ? 'bg-surface-secondary text-foreground' : 'text-muted hover:text-foreground'
              ].join(' ')}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </CardHeader>
      <CardContent className="space-y-2 px-6 pb-6">
        {items.map(item => (
          <div key={item.label} className="relative overflow-hidden rounded-lg">
            <div
              className={`absolute inset-y-0 left-0 rounded-lg ${barClass}`}
              style={{ width: `${item.percent}%` }}
              aria-hidden="true"
            />
            <div className="relative flex items-center justify-between gap-3 px-3 py-2 text-sm">
              <span className="flex min-w-0 items-center gap-2">
                <span aria-hidden="true">{item.emoji}</span>
                <span className="truncate text-foreground">{item.label}</span>
              </span>
              <span className="shrink-0 tabular-nums text-foreground">
                {formatMoney(item.amount, currency)}{' '}
                <span className="text-muted">({item.percent.toFixed(1)}%)</span>
              </span>
            </div>
          </div>
        ))}
      </CardContent>
    </Card>
  );
}
