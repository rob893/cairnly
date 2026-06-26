import type { ReactNode } from 'react';
import { Link } from 'react-router';
import { CairnMark } from './CairnMark';

interface AuthShellProps {
  /** Primary heading shown above the form (e.g. "Sign into your account"). */
  heading: string;
  /** Optional supporting line under the heading. */
  subheading?: string;
  /** Small prompt rendered in the top-right (e.g. a link to the opposite auth page). */
  topPrompt: ReactNode;
  /** Headline rendered on the decorative brand panel. */
  brandTitle: ReactNode;
  /** Supporting copy rendered on the brand panel. */
  brandSubtitle: string;
  /** The form (and any auxiliary links) for this auth page. */
  children: ReactNode;
}

/**
 * Split-screen authentication layout: a focused form column on the left and a
 * decorative, accent-driven brand panel on the right (hidden below `lg`). Shared
 * by the sign-in and register pages for a consistent, sophisticated entry point.
 */
export function AuthShell({ heading, subheading, topPrompt, brandTitle, brandSubtitle, children }: AuthShellProps) {
  return (
    <div className="min-h-screen bg-background text-foreground grid lg:grid-cols-2">
      {/* Form side */}
      <div className="relative flex flex-col px-6 sm:px-10 lg:px-16 py-7">
        <header className="flex items-center justify-between gap-4">
          <Link to="/" className="flex items-center gap-2 font-bold text-lg text-foreground no-underline">
            <CairnMark className="size-6 text-accent" />
            Cairnly
          </Link>
          <div className="flex items-center gap-3 text-sm text-muted">
            <span className="hidden sm:inline-flex items-center gap-1">{topPrompt}</span>
          </div>
        </header>

        <div className="flex-1 flex items-center py-10">
          <div className="w-full max-w-md mx-auto">
            <h1 className="text-3xl sm:text-4xl font-bold tracking-tight">{heading}</h1>
            {subheading && <p className="mt-3 text-muted">{subheading}</p>}
            <div className="mt-8">{children}</div>
            <div className="mt-8 sm:hidden text-sm text-muted">{topPrompt}</div>
          </div>
        </div>
      </div>

      {/* Brand panel */}
      <aside className="relative hidden lg:flex flex-col justify-end overflow-hidden bg-surface-secondary cairnly-aurora">
        <div className="absolute inset-0 cairnly-grid" aria-hidden="true" />
        <div className="cairnly-orb cairnly-float absolute -top-16 -right-10 size-72" aria-hidden="true" />
        <div
          className="cairnly-orb cairnly-float-slow absolute top-1/3 -left-16 size-80 opacity-70"
          aria-hidden="true"
        />

        {/* Floating cairn motif */}
        <div className="relative flex-1 flex items-center justify-center" aria-hidden="true">
          <div className="cairnly-float">
            <div className="rounded-3xl border border-border/60 bg-surface/40 backdrop-blur-xl p-12 shadow-2xl">
              <CairnMark className="size-40 text-accent drop-shadow-[0_8px_24px_color-mix(in_oklch,var(--accent)_45%,transparent)]" />
            </div>
          </div>
        </div>

        <div className="relative p-12 xl:p-16">
          <h2 className="text-3xl font-bold leading-snug max-w-md">{brandTitle}</h2>
          <p className="mt-4 text-muted max-w-md leading-relaxed">{brandSubtitle}</p>
        </div>
      </aside>
    </div>
  );
}
