import { Bar, CartesianGrid, ComposedChart, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import type { MockCashFlowPoint } from '../../constants/mockCashFlow';

interface CashFlowChartProps {
  data: MockCashFlowPoint[];
}

/** Compact axis/tooltip dollar formatter (e.g. 22000 -> "$22K"). */
function formatK(value: number): string {
  const abs = Math.abs(value);
  const sign = value < 0 ? '-' : '';

  if (abs >= 1000) {
    return `${sign}$${Math.round(abs / 1000)}K`;
  }

  return `${sign}$${abs}`;
}

interface TooltipPayloadItem {
  name: string;
  value: number;
  color: string;
}

/** Tooltip formatting each cash-flow series as a compact dollar figure. */
function ChartTooltip({
  active,
  payload,
  label
}: {
  active?: boolean;
  payload?: TooltipPayloadItem[];
  label?: string;
}) {
  if (!active || !payload?.length) {
    return null;
  }

  return (
    <div className="rounded-lg border border-border bg-overlay px-3 py-2 text-sm shadow-lg">
      <p className="mb-1 font-medium text-foreground">{label?.trim()}</p>
      {payload.map(item => (
        <div key={item.name} className="flex items-center gap-2 text-muted">
          <span className="size-2 rounded-full" style={{ backgroundColor: item.color }} aria-hidden="true" />
          <span className="capitalize">{item.name}</span>
          <span className="ml-auto tabular-nums text-foreground">{formatK(item.value)}</span>
        </div>
      ))}
    </div>
  );
}

/**
 * A combined chart of monthly income (green) and expenses (red) bars stacked
 * around the zero axis, with the net cash-flow line overlaid.
 */
export function CashFlowChart({ data }: CashFlowChartProps) {
  return (
    <div className="h-72 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <ComposedChart data={data} margin={{ top: 8, right: 8, left: 8, bottom: 0 }}>
          <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
          <XAxis
            dataKey="month"
            tick={{ fill: 'var(--muted)', fontSize: 12 }}
            tickLine={false}
            axisLine={{ stroke: 'var(--border)' }}
            interval={0}
            tickFormatter={(value: string) => value.trim()}
          />
          <YAxis
            tick={{ fill: 'var(--muted)', fontSize: 12 }}
            tickLine={false}
            axisLine={false}
            width={48}
            tickFormatter={formatK}
          />
          <Tooltip cursor={{ fill: 'var(--surface-secondary)', opacity: 0.4 }} content={<ChartTooltip />} />
          <Bar dataKey="income" stackId="cf" fill="var(--success)" maxBarSize={28} radius={[3, 3, 0, 0]} />
          <Bar dataKey="expenses" stackId="cf" fill="var(--danger)" maxBarSize={28} radius={[0, 0, 3, 3]} />
          <Line type="monotone" dataKey="net" stroke="var(--foreground)" strokeWidth={2} dot={false} />
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  );
}
