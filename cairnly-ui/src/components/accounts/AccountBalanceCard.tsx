import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { Card, CardContent, CardHeader, Spinner } from '@heroui/react';
import { SelectField } from '../SelectField';
import { formatMoney, minorToMajor } from '../../utils/money';
import { formatShortDate } from '../../utils/datetime';
import { TrendingDown, TrendingUp } from 'lucide-react';
import { balanceHistoryChangeLabel, balanceHistoryTimeframes } from '../../types/accounts';
import type { AccountBalancePoint, BalanceHistoryTimeframe } from '../../types/accounts';

interface AccountBalanceCardProps {
  /** The account's current balance in minor units. */
  currentBalance: number;
  currency: string;
  /** Balance history points for the selected window (minor units), ordered by day. */
  points: AccountBalancePoint[];
  timeframe: BalanceHistoryTimeframe;
  onTimeframeChange(timeframe: BalanceHistoryTimeframe): void;
  isLoading: boolean;
}

/** Compact axis tick formatter for minor-unit amounts (e.g. 2_264_609 -> "$22.6K"). */
function formatAxis(minorUnits: number, currency: string): string {
  const major = minorToMajor(minorUnits, currency);
  const abs = Math.abs(major);

  if (abs >= 1_000_000) {
    return `$${(major / 1_000_000).toFixed(2)}M`;
  }

  if (abs >= 1_000) {
    return `$${(major / 1_000).toFixed(1)}K`;
  }

  return `$${Math.round(major)}`;
}

/**
 * The account detail header card: the account's current balance, its change over
 * the selected window, and an area chart of the account's balance over time. The
 * series is anchored to the current balance as the final point.
 */
export function AccountBalanceCard({
  currentBalance,
  currency,
  points,
  timeframe,
  onTimeframeChange,
  isLoading
}: AccountBalanceCardProps) {
  const baseline = points.length > 0 ? points[0].balance : currentBalance;
  const change = currentBalance - baseline;
  const changePercent = baseline !== 0 ? (change / Math.abs(baseline)) * 100 : 0;
  const positive = change >= 0;

  const series = points.map(point => ({ date: formatShortDate(point.asOf), value: point.balance }));

  // Anchor the line to the current balance so the chart ends at "today".
  const today = formatShortDate(new Date().toISOString());
  if (series.length === 0 || series[series.length - 1].date !== today) {
    series.push({ date: today, value: currentBalance });
  } else {
    series[series.length - 1] = { date: today, value: currentBalance };
  }

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="flex flex-wrap items-start justify-between gap-4 px-6 pt-6">
        <div>
          <p className="text-xs font-semibold uppercase tracking-widest text-muted">Current balance</p>
          <div className="mt-1 flex flex-wrap items-center gap-3">
            <span className="text-3xl font-bold tabular-nums">{formatMoney(currentBalance, currency)}</span>
            {change !== 0 && (
              <span
                className={`flex items-center gap-1 text-sm font-medium ${positive ? 'text-success' : 'text-danger'}`}
              >
                {positive ? <TrendingUp className="size-4" /> : <TrendingDown className="size-4" />}
                {formatMoney(change, currency)} ({changePercent.toFixed(1)}%)
              </span>
            )}
            <span className="text-sm text-muted">{balanceHistoryChangeLabel[timeframe]}</span>
          </div>
        </div>
        <div className="w-36">
          <SelectField
            aria-label="Balance timeframe"
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
          ) : (
            <ResponsiveContainer width="100%" height="100%">
              <AreaChart data={series} margin={{ top: 8, right: 8, left: 8, bottom: 0 }}>
                <defs>
                  <linearGradient id="accountBalanceFill" x1="0" y1="0" x2="0" y2="1">
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
                  domain={['auto', 'auto']}
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
                  formatter={value => [formatMoney(Number(value), currency), 'Balance']}
                />
                <Area
                  type="monotone"
                  dataKey="value"
                  stroke="var(--accent)"
                  strokeWidth={2}
                  fill="url(#accountBalanceFill)"
                />
              </AreaChart>
            </ResponsiveContainer>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
