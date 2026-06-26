import { Card, CardContent, CardHeader, Spinner } from '@heroui/react';
import { formatMoney } from '../../utils/money';
import type { SpendingPlanSummary, CadenceAmounts } from '../../types/spendingPlans';

interface SpendingPlanSummaryCardProps {
  summary?: SpendingPlanSummary;
  currency: string;
  isLoading: boolean;
}

const columns: ReadonlyArray<{ key: keyof CadenceAmounts; label: string }> = [
  { key: 'daily', label: 'Daily' },
  { key: 'weekly', label: 'Weekly' },
  { key: 'monthly', label: 'Monthly' },
  { key: 'annual', label: 'Annual' }
];

/**
 * Displays a spendingPlan's normalized income, expenses, and remaining amounts across
 * daily / weekly / monthly / annual cadence columns.
 */
export function SpendingPlanSummaryCard({ summary, currency, isLoading }: SpendingPlanSummaryCardProps) {
  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6">
        <div>
          <h2 className="text-lg font-semibold">Summary</h2>
          <p className="text-sm text-muted">Income and expenses normalized across cadences.</p>
        </div>
      </CardHeader>
      <CardContent className="px-6 pb-6">
        {isLoading || !summary ? (
          <div className="flex justify-center py-10">
            <Spinner size="lg" color="accent" />
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full min-w-[32rem] text-sm">
              <thead>
                <tr className="text-left text-xs font-semibold uppercase tracking-wide text-muted">
                  <th className="py-2 pr-4 font-semibold">Cadence</th>
                  {columns.map(col => (
                    <th key={col.key} className="py-2 px-4 text-right font-semibold">
                      {col.label}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-border">
                <SummaryRow label="Income" amounts={summary.income} currency={currency} className="text-success" />
                <SummaryRow
                  label="Expenses"
                  amounts={summary.expenses}
                  currency={currency}
                  className="text-danger"
                />
                <RemainingRow amounts={summary.remaining} currency={currency} />
              </tbody>
            </table>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

function SummaryRow({
  label,
  amounts,
  currency,
  className
}: {
  label: string;
  amounts: CadenceAmounts;
  currency: string;
  className?: string;
}) {
  return (
    <tr>
      <td className="py-3 pr-4 font-medium">{label}</td>
      {columns.map(col => (
        <td key={col.key} className={`py-3 px-4 text-right tabular-nums ${className ?? ''}`}>
          {formatMoney(amounts[col.key], currency)}
        </td>
      ))}
    </tr>
  );
}

function RemainingRow({ amounts, currency }: { amounts: CadenceAmounts; currency: string }) {
  return (
    <tr className="border-t-2 border-border">
      <td className="py-3 pr-4 font-semibold">Remaining</td>
      {columns.map(col => {
        const value = amounts[col.key];
        const tone = value < 0 ? 'text-danger' : 'text-foreground';
        return (
          <td key={col.key} className={`py-3 px-4 text-right font-semibold tabular-nums ${tone}`}>
            {formatMoney(value, currency)}
          </td>
        );
      })}
    </tr>
  );
}
