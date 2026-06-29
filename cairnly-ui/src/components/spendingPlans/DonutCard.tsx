import { Card, CardContent, CardHeader } from '@heroui/react';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import { formatMoney } from '../../utils/money';
import { ChartModeDropdown } from './ChartModeDropdown';
import type { DonutMode, Slice } from './spendingPlanChartUtils';

const DONUT_MODE_OPTIONS: ReadonlyArray<{ id: DonutMode; label: string }> = [
  { id: 'source', label: 'By source' },
  { id: 'group', label: 'By group' },
  { id: 'category', label: 'By category' }
];

interface DonutTooltipPayloadItem {
  name: string;
  value: number;
}

/** A composition donut with a centered monthly total, legend, and grouping-mode selector. */
export function DonutCard({
  title,
  slices,
  currency,
  emptyText,
  mode,
  onModeChange,
  modeAriaLabel
}: {
  title: string;
  slices: Slice[];
  currency: string;
  emptyText: string;
  mode: DonutMode;
  onModeChange(mode: DonutMode): void;
  modeAriaLabel: string;
}) {
  const total = slices.reduce((sum, slice) => sum + slice.value, 0);

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6 flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
        <div>
          <h2 className="text-lg font-semibold">{title}</h2>
          <p className="text-sm text-muted">Normalized to a monthly cadence.</p>
        </div>
        <ChartModeDropdown
          options={DONUT_MODE_OPTIONS}
          value={mode}
          onChange={onModeChange}
          ariaLabel={modeAriaLabel}
        />
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
                  <span
                    className="size-2.5 shrink-0 rounded-full"
                    style={{ backgroundColor: slice.color }}
                    aria-hidden="true"
                  />
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
