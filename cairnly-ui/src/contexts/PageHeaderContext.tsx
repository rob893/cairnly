import { createContext, useCallback, useState } from 'react';
import type { ReactNode } from 'react';

/** The top-bar title and actions a page contributes to the app shell. */
export interface PageHeaderConfig {
  /** Title shown in the top bar; when absent the bar falls back to a greeting. */
  title?: ReactNode;
  /** Actions rendered on the right of the top bar. */
  actions?: ReactNode;
}

/** Current header config — consumed by the top bar. */
const PageHeaderStateContext = createContext<PageHeaderConfig>({});

/** Stable setter for the header config — consumed by pages. */
const PageHeaderSetContext = createContext<(config: PageHeaderConfig) => void>(() => {});

export { PageHeaderStateContext, PageHeaderSetContext };

/**
 * Provides page-header state to the app shell. The state and setter live in
 * separate contexts so that pages (which only consume the stable setter) do not
 * re-render when the header config changes — only the top bar does.
 */
export function PageHeaderProvider({ children }: { children: ReactNode }) {
  const [config, setConfig] = useState<PageHeaderConfig>({});
  const setHeader = useCallback((next: PageHeaderConfig) => setConfig(next), []);

  return (
    <PageHeaderSetContext.Provider value={setHeader}>
      <PageHeaderStateContext.Provider value={config}>{children}</PageHeaderStateContext.Provider>
    </PageHeaderSetContext.Provider>
  );
}
