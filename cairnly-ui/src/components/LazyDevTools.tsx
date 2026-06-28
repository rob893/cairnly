import { lazy, Suspense } from 'react';

/**
 * Dev-only React Query Devtools, lazily imported behind import.meta.env.DEV so it is
 * dead-code-eliminated from production bundles.
 */
const DevTools = import.meta.env.DEV
  ? lazy(() => import('./DevTools').then(m => ({ default: m.DevTools })))
  : (): null => null;

/**
 * Renders the React Query Devtools in development, and nothing in production.
 */
export function LazyDevTools(): React.ReactElement {
  return (
    <Suspense>
      <DevTools />
    </Suspense>
  );
}
