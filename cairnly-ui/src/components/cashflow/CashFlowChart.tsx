import { Bar, CartesianGrid, Cell, ComposedChart, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { minorToMajor } from '../../utils/money';
import type { CashFlowPoint } from '../../types/reports';

interface CashFlowChartProps {
  data: CashFlowPoint[];
  /** Chart variant. */
  view: 'bar' | 'line';
  /** Currency for minor-unit conversion. */
  currency: string;
  /** Currently selected period start (ISO); brightened while others dim. */
  selectedStart: string | null;
  /** Called when a period bucket is clicked. */
  onSelectPeriod(periodStart: string): void;
}

interface ChartDatum {
  periodStart: string;
  label: string;
  income: number;
  expenses: number;
  net: number;
}

/** Compact axis/tooltip dollar formatter (e.g. 22000 -> "$22K"). */
function formatK(value: number): string {
  const abs = Math.abs(value);
  const sign = value < 0 ? '-' : '';

  if (abs >= 1000) {
    return `${sign}$${Math.round(abs / 1000)}K`;
  }

  return `${sign}$${Math.round(abs)}`;
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
      <p className="mb-1 font-medium text-foreground">{label}</p>
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
 * A combined chart of period income (green) and expenses (red), with the net
 * cash-flow line overlaid. Bars/points are clickable to select a period slice.
 */
export function CashFlowChart({ data, view, currency, selectedStart, onSelectPeriod }: CashFlowChartProps) {
  const chartData: ChartDatum[] = data.map(point => ({
    periodStart: point.periodStart,
    label: point.label,
    income: minorToMajor(point.income, currency),
    expenses: -minorToMajor(point.expenses, currency),
    net: minorToMajor(point.net, currency)
  }));

  const handleClick = (state: { activeTooltipIndex?: number | string | null }) => {
    const raw = state?.activeTooltipIndex;
    const index = typeof raw === 'number' ? raw : typeof raw === 'string' ? Number(raw) : -1;
    if (index >= 0 && chartData[index]) {
      onSelectPeriod(chartData[index].periodStart);
    }
  };

  // Dim non-selected bars so the active slice stands out; full opacity when nothing is selected.
  const barOpacity = (periodStart: string): number => (!selectedStart || selectedStart === periodStart ? 1 : 0.3);

  return (
    <div className="h-72 w-full">
      <ResponsiveContainer width="100%" height="100%">
        <ComposedChart data={chartData} margin={{ top: 8, right: 8, left: 8, bottom: 0 }} onClick={handleClick}>
          <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
          <XAxis
            dataKey="label"
            tick={{ fill: 'var(--muted)', fontSize: 12 }}
            tickLine={false}
            axisLine={{ stroke: 'var(--border)' }}
            interval={0}
          />
          <YAxis
            tick={{ fill: 'var(--muted)', fontSize: 12 }}
            tickLine={false}
            axisLine={false}
            width={48}
            tickFormatter={formatK}
          />
          <Tooltip cursor={{ fill: 'var(--surface-secondary)', opacity: 0.4 }} content={<ChartTooltip />} />
          {view === 'bar' ? (
            <>
              <Bar
                dataKey="income"
                stackId="cf"
                fill="var(--success)"
                maxBarSize={28}
                radius={[3, 3, 0, 0]}
                className="cursor-pointer"
              >
                {chartData.map(d => (
                  <Cell key={d.periodStart} fillOpacity={barOpacity(d.periodStart)} />
                ))}
              </Bar>
              <Bar
                dataKey="expenses"
                stackId="cf"
                fill="var(--danger)"
                maxBarSize={28}
                radius={[0, 0, 3, 3]}
                className="cursor-pointer"
              >
                {chartData.map(d => (
                  <Cell key={d.periodStart} fillOpacity={barOpacity(d.periodStart)} />
                ))}
              </Bar>
              <Line type="monotone" dataKey="net" stroke="var(--foreground)" strokeWidth={2} dot={false} />
            </>
          ) : (
            <>
              <Line type="monotone" dataKey="income" stroke="var(--success)" strokeWidth={2} dot={false} />
              <Line type="monotone" dataKey="expenses" stroke="var(--danger)" strokeWidth={2} dot={false} />
              <Line type="monotone" dataKey="net" stroke="var(--foreground)" strokeWidth={2} dot={false} />
            </>
          )}
        </ComposedChart>
      </ResponsiveContainer>
    </div>
  );
}
