import { Bar, BarChart, CartesianGrid, Legend, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card, CardContent, CardHeader } from '@heroui/react';
import { SelectField } from '../SelectField';
import { formatMoney } from '../../utils/money';
import type { DashboardCadence } from '../../utils/dashboard';
import { dashboardCadenceOptions } from '../../utils/dashboard';

/** A single spendingPlan's normalized income/expenses for the charted cadence (major units). */
export interface SpendingDatum {
  name: string;
  income: number;
  expenses: number;
}

interface SpendingChartCardProps {
  data: SpendingDatum[];
  currency: string;
  cadence: DashboardCadence;
  onCadenceChange(cadence: DashboardCadence): void;
}

interface TooltipPayloadItem {
  name: string;
  value: number;
  color: string;
}

/** Tooltip that formats each series value as localized currency. */
function ChartTooltip({
  active,
  payload,
  label,
  currency
}: {
  active?: boolean;
  payload?: TooltipPayloadItem[];
  label?: string;
  currency: string;
}) {
  if (!active || !payload?.length) {
    return null;
  }

  const digits = currency.toUpperCase() === 'JPY' ? 0 : 2;

  return (
    <div className="rounded-lg border border-border bg-overlay px-3 py-2 text-sm shadow-lg">
      <p className="mb-1 font-medium text-foreground">{label}</p>
      {payload.map(item => (
        <div key={item.name} className="flex items-center gap-2 text-muted">
          <span className="size-2 rounded-full" style={{ backgroundColor: item.color }} aria-hidden="true" />
          <span className="capitalize">{item.name}</span>
          <span className="ml-auto tabular-nums text-foreground">
            {formatMoney(Math.round(item.value * 10 ** digits), currency)}
          </span>
        </div>
      ))}
    </div>
  );
}

/**
 * A bar chart comparing normalized income and expenses for each spendingPlan at the
 * selected cadence, with a cadence selector in the header.
 */
export function SpendingChartCard({ data, currency, cadence, onCadenceChange }: SpendingChartCardProps) {
  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="flex flex-wrap items-center justify-between gap-3 px-6 pt-6">
        <div>
          <h2 className="text-lg font-semibold">Income vs expenses</h2>
          <p className="text-sm text-muted">Normalized per spending plan at the selected cadence.</p>
        </div>
        <div className="w-40">
          <SelectField
            aria-label="Chart cadence"
            value={cadence}
            onChange={onCadenceChange}
            options={dashboardCadenceOptions}
          />
        </div>
      </CardHeader>
      <CardContent className="px-2 pb-4 sm:px-4">
        <div className="h-72 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <BarChart data={data} margin={{ top: 8, right: 8, left: 8, bottom: 0 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
              <XAxis
                dataKey="name"
                tick={{ fill: 'var(--muted)', fontSize: 12 }}
                tickLine={false}
                axisLine={{ stroke: 'var(--border)' }}
              />
              <YAxis tick={{ fill: 'var(--muted)', fontSize: 12 }} tickLine={false} axisLine={false} width={48} />
              <Tooltip
                cursor={{ fill: 'var(--surface-secondary)', opacity: 0.5 }}
                content={<ChartTooltip currency={currency} />}
              />
              <Legend wrapperStyle={{ fontSize: 12, textTransform: 'capitalize' }} iconType="circle" />
              <Bar dataKey="income" fill="var(--success)" radius={[4, 4, 0, 0]} maxBarSize={36} />
              <Bar dataKey="expenses" fill="var(--danger)" radius={[4, 4, 0, 0]} maxBarSize={36} />
            </BarChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  );
}
