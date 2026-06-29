import { useState } from 'react';
import { Card, CardContent } from '@heroui/react';
import { formatMoney } from '../../utils/money';
import type { CashFlowBreakdownItem, CashFlowBreakdowns, CashFlowDimension } from '../../types/reports';

const groupTabs: ReadonlyArray<{ id: CashFlowDimension; label: string }> = [
  { id: 'category', label: 'Category' },
  { id: 'group', label: 'Group' },
  { id: 'merchant', label: 'Merchant' }
];

const itemsFor: Record<CashFlowDimension, keyof CashFlowBreakdowns> = {
  category: 'byCategory',
  group: 'byGroup',
  merchant: 'byMerchant'
};

interface CategoryBreakdownProps {
  title: string;
  breakdowns: CashFlowBreakdowns;
  currency: string;
  /** Bar tone: income (green) or expense (red). */
  tone: 'income' | 'expense';
  /** Called when a drillable row is clicked. */
  onDrill(dimension: CashFlowDimension, item: CashFlowBreakdownItem): void;
}

/**
 * A section listing income or expense breakdowns as proportional horizontal
 * bars. Tabs switch between category, group, and merchant; clicking a row drills
 * into the matching transactions.
 */
export function CategoryBreakdown({ title, breakdowns, currency, tone, onDrill }: CategoryBreakdownProps) {
  const [dimension, setDimension] = useState<CashFlowDimension>('category');
  const barClass = tone === 'income' ? 'bg-success/25' : 'bg-danger/25';
  const items = breakdowns[itemsFor[dimension]];

  return (
    <Card className="bg-surface border border-border">
      <div className="flex w-full flex-wrap items-center justify-between gap-3 px-6 pt-6">
        <h2 className="text-lg font-semibold">{title}</h2>
        <div className="flex rounded-lg border border-border p-0.5 text-sm">
          {groupTabs.map(tab => (
            <button
              key={tab.id}
              type="button"
              onClick={() => setDimension(tab.id)}
              className={[
                'rounded-md px-3 py-1 transition-colors',
                dimension === tab.id ? 'bg-surface-secondary text-foreground' : 'text-muted hover:text-foreground'
              ].join(' ')}
            >
              {tab.label}
            </button>
          ))}
        </div>
      </div>
      <CardContent className="space-y-2 px-6 pb-6">
        {items.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted">No {title.toLowerCase()} in this period.</p>
        ) : (
          <div className="group/list space-y-2">
            {items.map(item => {
              const drillable = item.key !== '';

              return (
                <button
                  key={`${dimension}:${item.key}:${item.label}`}
                  type="button"
                  disabled={!drillable}
                  onClick={() => drillable && onDrill(dimension, item)}
                  className="relative block w-full overflow-hidden rounded-lg text-left opacity-100 transition-opacity enabled:hover:ring-1 enabled:hover:ring-border disabled:cursor-default group-hover/list:opacity-40 hover:!opacity-100"
                >
                  <div
                    className={`absolute inset-y-0 left-0 rounded-lg ${barClass}`}
                    style={{ width: `${item.percent}%` }}
                    aria-hidden="true"
                  />
                  <div className="relative flex items-center justify-between gap-3 px-3 py-2 text-sm">
                    <span className="flex min-w-0 items-center gap-2">
                      {item.icon ? <span aria-hidden="true">{item.icon}</span> : null}
                      <span className="truncate text-foreground">{item.label}</span>
                    </span>
                    <span className="shrink-0 tabular-nums text-foreground">
                      {formatMoney(item.amount, currency)}{' '}
                      <span className="text-muted">({item.percent.toFixed(1)}%)</span>
                    </span>
                  </div>
                </button>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
