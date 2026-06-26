import { Link } from 'react-router';
import { CairnMark } from './CairnMark';
import { ThemeToggle } from './ThemeToggle';

/**
 * The public marketing top bar for the landing page: brand mark plus theme
 * toggle and sign-in / sign-up actions for unauthenticated visitors.
 */
export function LandingHeader() {
  return (
    <header className="sticky top-0 z-40 w-full border-b border-border bg-surface/80 backdrop-blur">
      <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-4 px-6">
        <Link to="/" className="flex shrink-0 items-center gap-2 text-xl font-bold text-foreground no-underline">
          <CairnMark className="size-6 text-accent" />
          Cairnly
        </Link>

        <nav className="flex items-center gap-2">
          <ThemeToggle />
          <Link
            to="/login"
            className="px-3 py-1.5 text-sm font-medium text-muted transition-colors hover:text-foreground"
          >
            Sign In
          </Link>
          <Link
            to="/register"
            className="inline-flex items-center justify-center rounded-lg bg-accent px-3 py-1.5 text-sm font-semibold text-accent-foreground transition-opacity hover:opacity-90 no-underline"
          >
            Sign Up
          </Link>
        </nav>
      </div>
    </header>
  );
}
