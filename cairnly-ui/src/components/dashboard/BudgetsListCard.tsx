import { Link } from 'react-router';
import { Card, CardContent, CardHeader, Spinner } from '@heroui/react';
import { formatMoney } from '../../utils/money';
import { spendTone, spendToneBarClass, spentRatio } from '../../utils/dashboard';

/** A budget summarized for the dashboard list at the selected cadence (minor units). */
export interface BudgetListItem {
  id: number;
  name: string;
  currency: string;
  income: number;
  expenses: number;
  remaining: number;
}

interface BudgetsListCardProps {
  items: BudgetListItem[];
  isLoading: boolean;
}

/**
 * A scannable list of budgets, each with an income-vs-expenses progress bar and
 * remaining amount, linking through to the budget detail page.
 */
export function BudgetsListCard({ items, isLoading }: BudgetsListCardProps) {
  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="flex items-center justify-between gap-3 px-6 pt-6">
        <h2 className="text-lg font-semibold">Budgets</h2>
        <Link to="/budgets" className="text-sm font-medium text-accent no-underline hover:opacity-80">
          View all →
        </Link>
      </CardHeader>
      <CardContent className="px-6 pb-6">
        {isLoading ? (
          <div className="flex justify-center py-10">
            <Spinner size="lg" color="accent" />
          </div>
        ) : items.length === 0 ? (
          <p className="py-6 text-center text-sm text-muted">No budgets to show.</p>
        ) : (
          <ul className="space-y-5">
            {items.map(item => {
              const tone = spendTone(item.income, item.expenses);
              const ratio = spentRatio(item.income, item.expenses);
              const remainingTone = item.remaining < 0 ? 'text-danger' : 'text-foreground';

              return (
                <li key={item.id}>
                  <Link to={`/budgets/${item.id}`} className="group block no-underline">
                    <div className="flex items-baseline justify-between gap-3">
                      <span className="truncate font-medium text-foreground group-hover:text-accent">
                        {item.name}
                      </span>
                      <span className={`shrink-0 text-sm font-semibold tabular-nums ${remainingTone}`}>
                        {formatMoney(item.remaining, item.currency)}
                      </span>
                    </div>
                    <div className="mt-2 h-2 w-full overflow-hidden rounded-full bg-surface-secondary">
                      <div
                        className={`h-full rounded-full ${spendToneBarClass[tone]}`}
                        style={{ width: `${Math.round(ratio * 100)}%` }}
                      />
                    </div>
                    <div className="mt-1 flex justify-between text-xs text-muted">
                      <span>{formatMoney(item.expenses, item.currency)} spent</span>
                      <span>{formatMoney(item.income, item.currency)} income</span>
                    </div>
                  </Link>
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
