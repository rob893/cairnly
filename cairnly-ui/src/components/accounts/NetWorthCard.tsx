import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card, CardContent, CardHeader, Spinner } from '@heroui/react';
import { SelectField } from '../SelectField';
import { formatMoney, minorToMajor } from '../../utils/money';
import { formatShortDate } from '../../utils/datetime';
import { TrendingDown, TrendingUp } from 'lucide-react';
import { balanceHistoryChangeLabel, balanceHistoryTimeframes } from '../../types/accounts';
import type { BalanceHistoryTimeframe, NetWorthHistory } from '../../types/accounts';

interface NetWorthCardProps {
  history: NetWorthHistory | undefined;
  currency: string;
  timeframe: BalanceHistoryTimeframe;
  onTimeframeChange(timeframe: BalanceHistoryTimeframe): void;
  isLoading: boolean;
}

/** Compact axis tick formatter for minor-unit amounts (e.g. 125_000_000 -> "$1.25M"). */
function formatAxis(minorUnits: number, currency: string): string {
  const major = minorToMajor(minorUnits, currency);
  const abs = Math.abs(major);

  if (abs >= 1_000_000) {
    return `$${(major / 1_000_000).toFixed(2)}M`;
  }

  if (abs >= 1_000) {
    return `$${Math.round(major / 1_000)}K`;
  }

  return `$${Math.round(major)}`;
}

/**
 * The Accounts page header card: the current net worth, its change over the
 * selected window, and an area chart of net worth over time. Changing the
 * timeframe refetches the underlying history via {@link onTimeframeChange}.
 */
export function NetWorthCard({ history, currency, timeframe, onTimeframeChange, isLoading }: NetWorthCardProps) {
  const total = history?.netWorth ?? 0;
  const change = history?.change ?? 0;
  const changePercent = history?.changePercent ?? 0;
  const positive = change >= 0;

  const series = (history?.series ?? []).map(point => ({
    date: formatShortDate(point.asOf),
    value: point.netWorth
  }));

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="flex flex-wrap items-start justify-between gap-4 px-6 pt-6">
        <div>
          <p className="text-xs font-semibold uppercase tracking-widest text-muted">Net worth</p>
          <div className="mt-1 flex flex-wrap items-center gap-3">
            <span className="text-3xl font-bold tabular-nums">{formatMoney(total, currency)}</span>
            <span
              className={`flex items-center gap-1 text-sm font-medium ${positive ? 'text-success' : 'text-danger'}`}
            >
              {positive ? <TrendingUp className="size-4" /> : <TrendingDown className="size-4" />}
              {formatMoney(change, currency)} ({changePercent.toFixed(1)}%)
            </span>
            <span className="text-sm text-muted">{balanceHistoryChangeLabel[timeframe]}</span>
          </div>
        </div>
        <div className="w-36">
          <SelectField
            aria-label="Net worth timeframe"
            value={timeframe}
            onChange={onTimeframeChange}
            options={balanceHistoryTimeframes}
          />
        </div>
      </CardHeader>
      <CardContent className="px-2 pb-4 sm:px-4">
        <div className="h-64 w-full">
          {isLoading ? (
            <div className="flex h-full items-center justify-center">
              <Spinner size="lg" color="accent" />
            </div>
          ) : series.length === 0 ? (
            <div className="flex h-full items-center justify-center px-4 text-center text-sm text-muted">
              No net worth history yet. It builds up as you update balances and add transactions.
            </div>
          ) : (
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
                  tickFormatter={value => formatAxis(Number(value), currency)}
                />
                <Tooltip
                  cursor={{ stroke: 'var(--border)' }}
                  contentStyle={{
                    background: 'var(--overlay)',
                    border: '1px solid var(--border)',
                    borderRadius: 8,
                    color: 'var(--foreground)'
                  }}
                  formatter={value => [formatMoney(Number(value), currency), 'Net worth']}
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
          )}
        </div>
      </CardContent>
    </Card>
  );
}
