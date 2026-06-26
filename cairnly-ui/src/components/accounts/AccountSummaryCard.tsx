import { useState } from 'react';
import { Card, CardContent, CardHeader } from '@heroui/react';
import { formatMoney } from '../../utils/money';
import type { MockSummaryItem } from '../../constants/mockAccounts';

interface AccountSummaryCardProps {
  assets: MockSummaryItem[];
  liabilities: MockSummaryItem[];
  currency: string;
}

type DisplayMode = 'totals' | 'percent';

/** A stacked proportional bar built from the items' share of the section total. */
function SegmentBar({ items, total }: { items: MockSummaryItem[]; total: number }) {
  return (
    <div className="flex h-2 w-full overflow-hidden rounded-full bg-surface-secondary">
      {items.map(item => (
        <div
          key={item.label}
          className={item.color}
          style={{ width: total > 0 ? `${(item.amount / total) * 100}%` : '0%' }}
        />
      ))}
    </div>
  );
}

/** A labeled, color-coded list of summary line items. */
function SummaryList({
  items,
  total,
  mode,
  currency
}: {
  items: MockSummaryItem[];
  total: number;
  mode: DisplayMode;
  currency: string;
}) {
  return (
    <ul className="space-y-2.5">
      {items.map(item => (
        <li key={item.label} className="flex items-center gap-3 text-sm">
          <span className={`size-2 shrink-0 rounded-full ${item.color}`} aria-hidden="true" />
          <span className="text-muted">{item.label}</span>
          <span className="ml-auto tabular-nums text-foreground">
            {mode === 'totals'
              ? formatMoney(item.amount, currency)
              : `${total > 0 ? ((item.amount / total) * 100).toFixed(1) : '0.0'}%`}
          </span>
        </li>
      ))}
    </ul>
  );
}

/**
 * A breakdown of total assets and liabilities with a proportional bar and a
 * color-coded legend, toggleable between dollar totals and percentages.
 */
export function AccountSummaryCard({ assets, liabilities, currency }: AccountSummaryCardProps) {
  const [mode, setMode] = useState<DisplayMode>('totals');

  const assetsTotal = assets.reduce((sum, item) => sum + item.amount, 0);
  const liabilitiesTotal = liabilities.reduce((sum, item) => sum + item.amount, 0);

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="flex items-center justify-between gap-3 px-6 pt-6">
        <h2 className="text-lg font-semibold">Summary</h2>
        <div className="flex rounded-lg border border-border p-0.5 text-sm">
          {(['totals', 'percent'] as const).map(option => (
            <button
              key={option}
              type="button"
              onClick={() => setMode(option)}
              className={[
                'rounded-md px-3 py-1 capitalize transition-colors',
                mode === option ? 'bg-surface-secondary text-foreground' : 'text-muted hover:text-foreground'
              ].join(' ')}
            >
              {option}
            </button>
          ))}
        </div>
      </CardHeader>
      <CardContent className="space-y-6 px-6 pb-6">
        <div className="space-y-3">
          <div className="flex items-baseline justify-between">
            <span className="font-semibold text-foreground">Assets</span>
            <span className="tabular-nums text-muted">{formatMoney(assetsTotal, currency)}</span>
          </div>
          <SegmentBar items={assets} total={assetsTotal} />
          <SummaryList items={assets} total={assetsTotal} mode={mode} currency={currency} />
        </div>

        <div className="space-y-3">
          <div className="flex items-baseline justify-between">
            <span className="font-semibold text-foreground">Liabilities</span>
            <span className="tabular-nums text-muted">{formatMoney(liabilitiesTotal, currency)}</span>
          </div>
          <SegmentBar items={liabilities} total={liabilitiesTotal} />
          <SummaryList items={liabilities} total={liabilitiesTotal} mode={mode} currency={currency} />
        </div>
      </CardContent>
    </Card>
  );
}
