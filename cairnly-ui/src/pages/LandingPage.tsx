import type { CSSProperties, ReactNode } from 'react';
import { Link, Navigate } from 'react-router';
import { Spinner } from '@heroui/react';
import { useAuth } from '../hooks/useAuth';
import { LandingHeader } from '../components/LandingHeader';
import { CairnMark } from '../components/CairnMark';
import { Reveal } from '../components/Reveal';

/** A single accent-tinted product card with an abstract illustration and a label tab. */
interface FeatureCard {
  name: string;
  body: string;
  /** Background fill expressed with theme tokens. */
  tint: string;
  art: ReactNode;
}

function mix(accentPct: number, base = 'var(--surface)'): string {
  return `color-mix(in oklch, var(--accent) ${accentPct}%, ${base})`;
}

const featureCards: FeatureCard[] = [
  {
    name: 'Spending Plans',
    body: 'Plan income and expenses on any cadence and see it normalized into one clear number.',
    tint: mix(26),
    art: <SpendingPlanArt />
  },
  {
    name: 'Accounts',
    body: 'Track every account, asset, and liability — and watch your net worth accumulate.',
    tint: 'var(--surface-secondary)',
    art: <AccountsArt />
  },
  {
    name: 'Transactions',
    body: 'Log and categorize spending so every dollar is accounted for.',
    tint: mix(15),
    art: <TransactionsArt />
  },
  {
    name: 'Insights',
    body: 'Answer the only question that matters: “Am I on track this month?”',
    tint: 'var(--surface-tertiary)',
    art: <InsightsArt />
  },
  {
    name: 'Milestones',
    body: 'Stack small, consistent actions into real, observable progress over time.',
    tint: mix(34),
    art: <MilestonesArt />
  }
];

const steps = [
  {
    title: 'Connect your picture',
    body: 'Add your accounts, assets, and liabilities to establish a single, honest baseline.'
  },
  {
    title: 'Plan your cadence',
    body: 'Set spending plans on whatever rhythm fits your life — weekly, monthly, or annual.'
  },
  {
    title: 'Watch it compound',
    body: 'Categorize spending and follow your net worth climb, one marker at a time.'
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
    <div className="min-h-screen bg-background text-foreground flex flex-col overflow-x-hidden">
      <LandingHeader />

      <main className="flex-1">
        {/* Hero */}
        <section className="relative isolate overflow-hidden">
          <div className="absolute inset-0 -z-20 cairnly-aurora" aria-hidden="true" />
          <div className="absolute inset-0 -z-10 cairnly-grid" aria-hidden="true" />
          <div className="cairnly-orb cairnly-float absolute -top-20 left-1/4 size-72 opacity-60" aria-hidden="true" />
          <div
            className="cairnly-orb cairnly-float-slow absolute top-40 right-1/5 size-80 opacity-50"
            aria-hidden="true"
          />

          <div className="mx-auto max-w-4xl px-6 pt-24 pb-28 text-center sm:pt-32">
            <Reveal from="none">
              <span className="inline-flex items-center gap-2 rounded-full border border-border bg-surface/70 px-4 py-1.5 text-xs font-medium text-muted backdrop-blur">
                <CairnMark className="size-4 text-accent" />
                Personal finance, the observable way
              </span>
            </Reveal>

            <Reveal delay={80}>
              <h1 className="mt-8 text-5xl font-bold tracking-tight sm:text-7xl">
                The calm way to build
                <br />
                <span className="cairnly-text-gradient">financial clarity.</span>
              </h1>
            </Reveal>

            <Reveal delay={160}>
              <p className="mx-auto mt-6 max-w-2xl text-lg text-muted">
                Cairnly helps you understand, plan, and optimize your money — spending plans, accounts, and spending in
                one clear place. Build wealth one marker at a time.
              </p>
            </Reveal>

            <Reveal delay={240}>
              <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row">
                <Link
                  to="/register"
                  className="inline-flex w-full items-center justify-center rounded-xl bg-accent px-7 py-3.5 text-base font-semibold text-accent-foreground shadow-lg shadow-accent/20 transition-transform hover:-translate-y-0.5 sm:w-auto"
                >
                  Get started — it's free
                </Link>
                <Link
                  to="/login"
                  className="inline-flex w-full items-center justify-center rounded-xl border border-border bg-surface/70 px-7 py-3.5 text-base font-semibold text-foreground backdrop-blur transition-colors hover:bg-surface-secondary sm:w-auto"
                >
                  Sign in
                </Link>
              </div>
            </Reveal>
          </div>
        </section>

        {/* Statement + feature cards */}
        <section className="relative mx-auto w-full max-w-[2000px] px-6 lg:px-10 pb-28">
          <Reveal from="left">
            <h2 className="max-w-3xl text-3xl font-bold tracking-tight sm:text-5xl">
              Everything you need to track spend and grow wealth — <span className="text-muted">in one place.</span>
            </h2>
          </Reveal>

          <div className="mt-12 grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3">
            {featureCards.map((card, index) => (
              <Reveal key={card.name} delay={index * 90} className={index === 4 ? 'sm:col-span-2 lg:col-span-1' : ''}>
                <FeatureCardView card={card} />
              </Reveal>
            ))}
          </div>
        </section>

        {/* How it works */}
        <section className="relative w-full border-t border-border bg-surface-secondary/40">
          <div className="mx-auto w-full max-w-[2000px] px-6 lg:px-10 py-24">
            <Reveal>
              <p className="text-sm font-semibold uppercase tracking-widest text-accent">How it works</p>
              <h2 className="mt-3 max-w-2xl text-3xl font-bold tracking-tight sm:text-4xl">
                Three steps to a clearer financial picture.
              </h2>
            </Reveal>

            <div className="mt-14 grid grid-cols-1 gap-8 md:grid-cols-3">
              {steps.map((step, index) => (
                <Reveal key={step.title} delay={index * 120}>
                  <div className="relative h-full rounded-2xl border border-border bg-surface p-7">
                    <div className="flex size-11 items-center justify-center rounded-xl bg-accent/10 text-lg font-bold text-accent">
                      {index + 1}
                    </div>
                    <h3 className="mt-5 text-lg font-semibold">{step.title}</h3>
                    <p className="mt-2 text-sm leading-relaxed text-muted">{step.body}</p>
                  </div>
                </Reveal>
              ))}
            </div>
          </div>
        </section>

        {/* Final CTA */}
        <section className="relative w-full overflow-hidden">
          <div className="absolute inset-0 -z-10 cairnly-aurora opacity-80" aria-hidden="true" />
          <div
            className="cairnly-orb cairnly-float absolute bottom-0 right-1/4 size-72 opacity-50"
            aria-hidden="true"
          />
          <div className="mx-auto w-full max-w-4xl px-6 py-28 text-center">
            <Reveal>
              <CairnMark className="mx-auto size-12 text-accent" />
              <h2 className="mt-6 text-4xl font-bold tracking-tight sm:text-5xl">
                Start building your <span className="cairnly-text-gradient">cairn</span> today.
              </h2>
              <p className="mx-auto mt-5 max-w-xl text-lg text-muted">
                Free to start. No spreadsheets. Just a calm, clear view of your money for the long journey.
              </p>
              <Link
                to="/register"
                className="mt-9 inline-flex items-center justify-center rounded-xl bg-accent px-8 py-3.5 text-base font-semibold text-accent-foreground shadow-lg shadow-accent/20 transition-transform hover:-translate-y-0.5"
              >
                Create your free account
              </Link>
            </Reveal>
          </div>
        </section>
      </main>

      <footer className="border-t border-border">
        <div className="mx-auto flex w-full max-w-[2000px] flex-col items-center justify-between gap-3 px-6 lg:px-10 py-8 text-sm text-muted sm:flex-row">
          <span className="flex items-center gap-2">
            <CairnMark className="size-4 text-accent" />
            Cairnly — financial clarity for the long journey.
          </span>
          <div className="flex items-center gap-5">
            <Link to="/login" className="transition-colors hover:text-foreground">
              Sign in
            </Link>
            <Link to="/register" className="transition-colors hover:text-foreground">
              Get started
            </Link>
          </div>
        </div>
      </footer>
    </div>
  );
}

function FeatureCardView({ card }: { card: FeatureCard }) {
  return (
    <article
      className="group flex h-full flex-col overflow-hidden rounded-2xl border border-border/60 shadow-sm transition-transform duration-300 hover:-translate-y-1"
      style={{ backgroundColor: card.tint }}
    >
      <div className="flex flex-1 flex-col p-6">
        <p className="text-sm leading-relaxed text-foreground/80">{card.body}</p>
        <div className="mt-6 flex flex-1 items-end">{card.art}</div>
      </div>
      <div className="flex items-center justify-between border-t border-border/40 bg-background/30 px-6 py-4 backdrop-blur-sm">
        <span className="text-base font-semibold">{card.name}</span>
        <span className="text-accent transition-transform duration-300 group-hover:translate-x-1" aria-hidden="true">
          →
        </span>
      </div>
    </article>
  );
}

/* --- Abstract, theme-aware illustrations built from CSS (no images) --- */

function SpendingPlanArt() {
  const rows = [82, 58, 40];
  return (
    <div className="w-full space-y-3">
      {rows.map((pct, i) => (
        <div key={i} className="space-y-1.5">
          <div className="h-2 w-full overflow-hidden rounded-full bg-foreground/10">
            <div className="h-full rounded-full bg-accent" style={{ width: `${pct}%`, opacity: 1 - i * 0.25 }} />
          </div>
        </div>
      ))}
    </div>
  );
}

function AccountsArt() {
  return (
    <div className="w-full space-y-2.5">
      {[0, 1, 2].map(i => (
        <div key={i} className="flex items-center gap-3 rounded-lg bg-background/40 px-3 py-2.5">
          <span className="size-6 rounded-md bg-accent/20" />
          <span className="h-2 flex-1 rounded-full bg-foreground/10" />
          <span className="h-2 w-10 rounded-full bg-foreground/20" />
        </div>
      ))}
    </div>
  );
}

function TransactionsArt() {
  const bars = [30, 55, 38, 72, 48, 90, 64];
  return (
    <div className="flex h-24 w-full items-end gap-2">
      {bars.map((h, i) => (
        <div
          key={i}
          className="flex-1 rounded-t-md bg-accent"
          style={{ height: `${h}%`, opacity: 0.45 + (h / 100) * 0.55 }}
        />
      ))}
    </div>
  );
}

function InsightsArt() {
  const donut: CSSProperties = {
    background: 'conic-gradient(var(--accent) 0 62%, color-mix(in oklch, var(--foreground) 14%, transparent) 62% 100%)'
  };
  return (
    <div className="flex w-full items-center gap-4">
      <div className="relative size-20 rounded-full" style={donut}>
        <div className="absolute inset-[22%] rounded-full bg-background/60 backdrop-blur-sm" />
      </div>
      <div className="flex-1 space-y-2">
        <span className="block h-2 w-3/4 rounded-full bg-foreground/15" />
        <span className="block h-2 w-1/2 rounded-full bg-foreground/10" />
        <span className="block h-2 w-2/3 rounded-full bg-accent/40" />
      </div>
    </div>
  );
}

function MilestonesArt() {
  const stones = [
    { w: '92%', o: 0.95 },
    { w: '70%', o: 0.8 },
    { w: '50%', o: 0.65 },
    { w: '32%', o: 0.5 }
  ];
  return (
    <div className="flex w-full flex-col items-center gap-2">
      {stones.map((s, i) => (
        <div key={i} className="h-4 rounded-full bg-accent" style={{ width: s.w, opacity: s.o }} />
      ))}
    </div>
  );
}
