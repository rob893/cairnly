import { useState } from 'react';
import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card, CardContent, CardHeader } from '@heroui/react';
import { SelectField, type SelectOption } from '../SelectField';
import { formatMoney } from '../../utils/money';
import { TrendingUp } from 'lucide-react';

interface NetWorthCardProps {
  total: number;
  change: number;
  changePercent: number;
  series: ReadonlyArray<{ date: string; value: number }>;
  currency: string;
}

type Timeframe = '1m' | '3m' | '1y' | 'all';

const timeframeOptions: ReadonlyArray<SelectOption<Timeframe>> = [
  { value: '1m', label: '1 month' },
  { value: '3m', label: '3 months' },
  { value: '1y', label: '1 year' },
  { value: 'all', label: 'All time' }
];

/** Compact axis tick formatter (e.g. 1_250_000 -> "$1.25M"). */
function formatAxis(value: number): string {
  if (value >= 1_000_000) {
    return `$${(value / 1_000_000).toFixed(2)}M`;
  }

  if (value >= 1_000) {
    return `$${Math.round(value / 1_000)}K`;
  }

  return `$${value}`;
}

/**
 * The Accounts page header card: the current net worth, its period change, and an
 * area chart of net worth over time. The selectors are presentational
 * (placeholder data does not change with the timeframe).
 */
export function NetWorthCard({ total, change, changePercent, series, currency }: NetWorthCardProps) {
  const [timeframe, setTimeframe] = useState<Timeframe>('1m');
  const positive = change >= 0;

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="flex flex-wrap items-start justify-between gap-4 px-6 pt-6">
        <div>
          <p className="text-xs font-semibold uppercase tracking-widest text-muted">Net worth</p>
          <div className="mt-1 flex flex-wrap items-center gap-3">
            <span className="text-3xl font-bold tabular-nums">{formatMoney(total, currency)}</span>
            <span className={`flex items-center gap-1 text-sm font-medium ${positive ? 'text-success' : 'text-danger'}`}>
              <TrendingUp className="size-4" />
              {formatMoney(change, currency)} ({changePercent.toFixed(1)}%)
            </span>
            <span className="text-sm text-muted">1 month change</span>
          </div>
        </div>
        <div className="w-36">
          <SelectField
            aria-label="Net worth timeframe"
            value={timeframe}
            onChange={value => setTimeframe(value as Timeframe)}
            options={timeframeOptions}
          />
        </div>
      </CardHeader>
      <CardContent className="px-2 pb-4 sm:px-4">
        <div className="h-64 w-full">
          <ResponsiveContainer width="100%" height="100%">
            <AreaChart data={series} margin={{ top: 8, right: 8, left: 8, bottom: 0 }}>
              <defs>
                <linearGradient id="netWorthFill" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="var(--accent)" stopOpacity={0.35} />
                  <stop offset="100%" stopColor="var(--accent)" stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="var(--border)" vertical={false} />
              <XAxis
                dataKey="date"
                tick={{ fill: 'var(--muted)', fontSize: 12 }}
                tickLine={false}
                axisLine={{ stroke: 'var(--border)' }}
              />
              <YAxis
                tick={{ fill: 'var(--muted)', fontSize: 12 }}
                tickLine={false}
                axisLine={false}
                width={56}
                tickFormatter={formatAxis}
              />
              <Tooltip
                cursor={{ stroke: 'var(--border)' }}
                contentStyle={{
                  background: 'var(--overlay)',
                  border: '1px solid var(--border)',
                  borderRadius: 8,
                  color: 'var(--foreground)'
                }}
                formatter={(value) => [formatMoney(Math.round(Number(value) * 100), currency), 'Net worth']}
              />
              <Area
                type="monotone"
                dataKey="value"
                stroke="var(--accent)"
                strokeWidth={2}
                fill="url(#netWorthFill)"
              />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      </CardContent>
    </Card>
  );
}
