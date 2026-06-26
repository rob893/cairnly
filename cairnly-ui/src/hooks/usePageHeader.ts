import { useContext, useEffect } from 'react';
import {
  PageHeaderSetContext,
  PageHeaderStateContext,
  type PageHeaderConfig
} from '../contexts/PageHeaderContext';

/** Reads the current page-header config (used by the app top bar). */
export function usePageHeaderState(): PageHeaderConfig {
  return useContext(PageHeaderStateContext);
}

/**
 * Sets the top-bar title and actions for the lifetime of the calling page,
 * clearing them on unmount. Pages do not re-render when the header changes, so
 * inline JSX is safe to pass.
 *
 * @param config The title and/or actions to display in the top bar.
 */
export function usePageHeader(config: PageHeaderConfig): void {
  const setHeader = useContext(PageHeaderSetContext);
  const { title, actions } = config;

  useEffect(() => {
    setHeader({ title, actions });
    return () => setHeader({});
  }, [setHeader, title, actions]);
}
