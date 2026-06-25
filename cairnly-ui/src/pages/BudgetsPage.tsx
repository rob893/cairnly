import { BudgetsSection } from '../components/budgets/BudgetsSection';

export function BudgetsPage() {
  return (
    <div className="space-y-8">
      <div className="relative overflow-hidden rounded-2xl border border-border bg-surface-secondary/40 p-8 cairnly-aurora">
        <p className="text-sm font-semibold uppercase tracking-widest text-accent">Planning</p>
        <h1 className="mt-2 text-3xl font-bold tracking-tight">Budgets</h1>
        <p className="text-muted mt-1">Plan income and expenses across any cadence and watch your remaining budget.</p>
      </div>

      <BudgetsSection />
    </div>
  );
}
