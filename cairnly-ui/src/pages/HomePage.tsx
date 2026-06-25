import { Link } from 'react-router';
import { Card, CardContent, Chip } from '@heroui/react';
import { useAuth } from '../hooks/useAuth';

interface OverviewCard {
  title: string;
  description: string;
  to?: string;
  comingSoon?: boolean;
}

const overviewCards: OverviewCard[] = [
  {
    title: 'Accounts',
    description: 'Track checking, savings, brokerage, and more — assets and liabilities in one place.',
    comingSoon: true
  },
  {
    title: 'Budgets',
    description: 'Plan income and expenses across any cadence and watch your remaining budget.',
    to: '/budgets'
  },
  {
    title: 'Transactions',
    description: 'Log and categorize spending so every dollar is accounted for.',
    comingSoon: true
  },
  {
    title: 'Account & preferences',
    description: 'Manage your profile, linked logins, and theme.',
    to: '/account'
  }
];

export function HomePage() {
  const { user } = useAuth();

  return (
    <div className="space-y-8">
      <div className="relative overflow-hidden rounded-2xl border border-border bg-surface-secondary/40 p-8 cairnly-aurora">
        <p className="text-sm font-semibold uppercase tracking-widest text-accent">Dashboard</p>
        <h1 className="mt-2 text-3xl font-bold tracking-tight">Welcome back, {user?.userName ?? 'there'}.</h1>
        <p className="text-muted mt-1">Here's your financial home base.</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5">
        {overviewCards.map(card => {
          const inner = (
            <Card className="h-full bg-surface border border-border transition-colors hover:border-accent/50">
              <CardContent className="p-6 flex flex-col gap-2 h-full">
                <div className="flex items-center justify-between gap-2">
                  <h2 className="text-lg font-semibold">{card.title}</h2>
                  {card.comingSoon && (
                    <Chip variant="soft" size="sm">
                      Coming soon
                    </Chip>
                  )}
                </div>
                <p className="text-sm text-muted leading-relaxed">{card.description}</p>
                {card.to && <span className="mt-auto text-sm font-medium text-accent">Manage →</span>}
              </CardContent>
            </Card>
          );

          return card.to ? (
            <Link key={card.title} to={card.to} className="no-underline">
              {inner}
            </Link>
          ) : (
            <div key={card.title}>{inner}</div>
          );
        })}
      </div>
    </div>
  );
}
