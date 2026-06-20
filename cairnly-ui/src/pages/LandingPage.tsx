import { Link, Navigate } from 'react-router';
import { Card, CardContent, Spinner } from '@heroui/react';
import { useAuth } from '../hooks/useAuth';
import { AppHeader } from '../components/AppHeader';

const features = [
  {
    title: 'Budgets that fit your life',
    body: 'Plan income and expenses on any cadence — weekly groceries, monthly bills, annual gifts — and see it normalized into one clear picture.'
  },
  {
    title: 'Accounts & net worth',
    body: 'Track every account, asset and liability in one place and watch your wealth accumulate over time.'
  },
  {
    title: 'Spending insights',
    body: 'Categorize transactions and answer the only question that matters: “Am I on track this month?”'
  },
  {
    title: 'Milestones, not spreadsheets',
    body: 'Treat your finances like an observable system. Small consistent actions stack into real progress.'
  }
];

export function LandingPage() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <Spinner size="lg" color="accent" />
      </div>
    );
  }

  if (isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <div className="min-h-screen bg-background text-foreground flex flex-col">
      <AppHeader />

      <main className="flex-1">
        {/* Hero */}
        <section className="relative overflow-hidden">
          <div className="absolute inset-0 -z-10 bg-gradient-to-b from-surface-secondary/60 to-background" />
          <div className="max-w-5xl mx-auto px-4 py-24 text-center">
            <span className="inline-block rounded-full border border-border bg-surface px-3 py-1 text-xs font-medium text-muted mb-6">
              Personal finance, the observable way
            </span>
            <h1 className="text-4xl sm:text-6xl font-bold tracking-tight">
              Financial clarity for the <span className="text-accent">long journey</span>
            </h1>
            <p className="mt-6 text-lg text-muted max-w-2xl mx-auto">
              Cairnly helps you understand, plan, and optimize your money — budgets, accounts, and
              spending in one calm, clear place. Build wealth one marker at a time.
            </p>
            <div className="mt-10 flex flex-col sm:flex-row items-center justify-center gap-3">
              <Link
                to="/register"
                className="inline-flex items-center justify-center rounded-lg bg-accent px-6 py-3 text-base font-semibold text-accent-foreground hover:opacity-90 transition-opacity w-full sm:w-auto"
              >
                Get started — it's free
              </Link>
              <Link
                to="/login"
                className="inline-flex items-center justify-center rounded-lg border border-border bg-surface px-6 py-3 text-base font-semibold text-foreground hover:bg-surface-secondary transition-colors w-full sm:w-auto"
              >
                Sign in
              </Link>
            </div>
          </div>
        </section>

        {/* Features */}
        <section className="max-w-6xl mx-auto px-4 pb-24">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            {features.map(feature => (
              <Card key={feature.title} className="bg-surface border border-border">
                <CardContent className="p-6">
                  <h2 className="text-lg font-semibold mb-2">{feature.title}</h2>
                  <p className="text-muted text-sm leading-relaxed">{feature.body}</p>
                </CardContent>
              </Card>
            ))}
          </div>
        </section>
      </main>

      <footer className="border-t border-border">
        <div className="max-w-6xl mx-auto px-4 py-8 text-center text-sm text-muted">
          Cairnly — financial clarity for the long journey.
        </div>
      </footer>
    </div>
  );
}
