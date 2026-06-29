import { useEffect, useMemo, useState } from 'react';
import { Link, useParams, useSearchParams } from 'react-router';
import { Card, CardContent, Spinner } from '@heroui/react';
import { CashFlowChart } from '../components/cashflow/CashFlowChart';
import { TransactionsTable } from '../components/transactions/TransactionsTable';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { usePageHeader } from '../hooks/usePageHeader';
import { useCashFlowReport } from '../hooks/reports';
import { useCategories } from '../hooks/categories';
import { useTransactions, type TransactionFilters } from '../hooks/transactions';
import { showErrorDetails } from '../utils/environment';
import { keyAmountByPeriod, periodEndIso, seriesRange } from '../utils/cashflow';
import { formatMoney } from '../utils/money';
import { routePaths } from '../constants/routes';
import type { BalanceHistoryTimeframe } from '../types/accounts';
import type { CashFlowDimension, CashFlowPeriod, CashFlowPoint } from '../types/reports';

interface SummaryRowProps {
  label: string;
  value: string;
}

function SummaryRow({ label, value }: SummaryRowProps) {
  return (
    <div className="flex items-center justify-between gap-3 text-sm">
      <span className="text-muted">{label}</span>
      <span className="tabular-nums text-foreground">{value}</span>
    </div>
  );
}

/**
 * The cash-flow drill-down page for a single category, group, or merchant: a
 * per-period chart of that slice, its transactions for the selected period, and
 * a summary panel. Reached by clicking a breakdown bar on the Cash Flow page.
 */
export function CashFlowDetailPage() {
  const params = useParams<{ dimension: string; key: string }>();
  const [search] = useSearchParams();
  const dimension = (params.dimension ?? 'category') as CashFlowDimension;
  const key = params.key ?? '';
  const section = (search.get('section') ?? 'expense') as 'income' | 'expense';
  const label = search.get('label') ?? key;
  const timeframe = (search.get('timeframe') ?? 'OneYear') as BalanceHistoryTimeframe;
  const period = (search.get('period') ?? 'Monthly') as CashFlowPeriod;

  const [selectedStart, setSelectedStart] = useState<string | null>(search.get('from'));

  const report = useCashFlowReport(timeframe, period);
  const { groups } = useCategories();
  const currency = report.data?.currency ?? 'USD';
  const series = useMemo(() => report.data?.series ?? [], [report.data]);

  const keyPoints = useMemo(
    () => keyAmountByPeriod(series, section, dimension, key),
    [series, section, dimension, key]
  );
  const chartData = useMemo<CashFlowPoint[]>(
    () =>
      keyPoints.map(point => ({
        periodStart: point.periodStart,
        label: point.label,
        income: section === 'income' ? point.amount : 0,
        expenses: section === 'expense' ? point.amount : 0,
        net: section === 'income' ? point.amount : -point.amount,
        incomeBreakdowns: { byCategory: [], byGroup: [], byMerchant: [] },
        expenseBreakdowns: { byCategory: [], byGroup: [], byMerchant: [] }
      })),
    [keyPoints, section]
  );

  const selectedPoint = useMemo(() => series.find(p => p.periodStart === selectedStart), [series, selectedStart]);
  const range = useMemo(() => seriesRange(series, period), [series, period]);
  const dateFrom = selectedPoint ? selectedPoint.periodStart : range?.from;
  const dateTo = selectedPoint ? periodEndIso(selectedPoint.periodStart, period) : range?.to;

  const filters = useMemo<TransactionFilters>(() => {
    const base: TransactionFilters = { dateFrom, dateTo };
    if (dimension === 'merchant') {
      return { ...base, merchant: label };
    }
    if (dimension === 'category') {
      return { ...base, categoryId: Number(key) };
    }
    const parentId = Number(key);
    const group = groups.find(g => g.parent.id === parentId);
    return { ...base, categoryIds: group ? [parentId, ...group.children.map(c => c.id)] : [parentId] };
  }, [dimension, key, label, dateFrom, dateTo, groups]);

  const txQuery = useTransactions(filters);
  useEffect(() => {
    if (txQuery.hasNextPage && !txQuery.isFetchingNextPage) {
      void txQuery.fetchNextPage();
    }
  }, [txQuery.hasNextPage, txQuery.isFetchingNextPage, txQuery]);

  const transactions = useMemo(() => txQuery.data?.pages.flatMap(p => p.nodes ?? []) ?? [], [txQuery.data]);
  const summary = useMemo(() => {
    const total = transactions.reduce((sum, t) => sum + Math.abs(t.amount), 0);
    const largest = transactions.reduce((max, t) => Math.max(max, Math.abs(t.amount)), 0);
    return { count: transactions.length, total, largest, avg: transactions.length ? total / transactions.length : 0 };
  }, [transactions]);

  const header = useMemo(
    () => ({
      title: (
        <span className="flex items-center gap-2">
          <Link
            to={routePaths.cashFlow}
            aria-label="Back to cash flow"
            className="text-muted no-underline transition-colors hover:text-foreground"
          >
            ←
          </Link>
          <span className="text-muted">Cash Flow</span>
          <span className="text-muted">/</span>
          {label}
        </span>
      )
    }),
    [label]
  );
  usePageHeader(header);

  if (report.isLoading) {
    return (
      <div className="flex justify-center py-16">
        <Spinner size="lg" color="accent" />
      </div>
    );
  }

  if (report.isError) {
    return <ApiErrorDisplay error={report.error} showDetails={showErrorDetails} />;
  }

  return (
    <div className="space-y-6">
      <Card className="bg-surface border border-border">
        <CardContent className="space-y-4 p-4 sm:p-6">
          <CashFlowChart
            data={chartData}
            view="bar"
            currency={currency}
            selectedStart={selectedStart}
            onSelectPeriod={setSelectedStart}
          />
          <div className="border-t border-border pt-4">
            <h2 className="text-lg font-semibold">{selectedPoint ? selectedPoint.label : 'All periods'}</h2>
          </div>
        </CardContent>
      </Card>

      <div className="grid gap-6 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <TransactionsTable filters={filters} currency={currency} showToolbar={false} />
        </div>
        <Card className="bg-surface h-fit border border-border">
          <CardContent className="space-y-3 p-5">
            <h3 className="font-semibold">Summary</h3>
            <SummaryRow label="Total Transactions" value={String(summary.count)} />
            <SummaryRow label="Average Transaction" value={formatMoney(Math.round(summary.avg), currency)} />
            <SummaryRow label="Largest Transaction" value={formatMoney(summary.largest, currency)} />
            <SummaryRow label="Total Amount" value={formatMoney(summary.total, currency)} />
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
