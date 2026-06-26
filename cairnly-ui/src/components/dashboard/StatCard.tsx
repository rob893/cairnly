import type { ReactNode } from 'react';
import { Card, CardContent } from '@heroui/react';

/** Visual tone applied to a {@link StatCard}'s icon badge and value. */
export type StatTone = 'income' | 'expense' | 'neutral';

const toneClasses: Record<StatTone, { badge: string; value: string }> = {
  income: { badge: 'bg-success/15 text-success', value: 'text-success' },
  expense: { badge: 'bg-danger/15 text-danger', value: 'text-danger' },
  neutral: { badge: 'bg-accent/15 text-accent', value: 'text-foreground' }
};

interface StatCardProps {
  /** Short metric label (e.g. "Monthly income"). */
  label: string;
  /** Formatted metric value (e.g. "$4,200.00"). */
  value: string;
  /** Optional supporting text under the value. */
  hint?: string;
  /** Icon rendered in the colored badge. */
  icon: ReactNode;
  /** Color treatment for the badge and value. */
  tone?: StatTone;
}

/** A compact summary tile showing a single labeled monetary metric. */
export function StatCard({ label, value, hint, icon, tone = 'neutral' }: StatCardProps) {
  const classes = toneClasses[tone];

  return (
    <Card className="bg-surface border border-border">
      <CardContent className="flex items-center gap-4 p-5">
        <div className={`flex size-11 shrink-0 items-center justify-center rounded-xl ${classes.badge}`}>{icon}</div>
        <div className="min-w-0">
          <p className="text-sm text-muted">{label}</p>
          <p className={`truncate text-xl font-semibold tabular-nums ${classes.value}`}>{value}</p>
          {hint && <p className="truncate text-xs text-muted">{hint}</p>}
        </div>
      </CardContent>
    </Card>
  );
}
