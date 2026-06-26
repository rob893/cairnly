import { useEffect, useRef } from 'react';
import { useAuth } from '../hooks/useAuth';
import { useTheme } from '../hooks/useTheme';
import { usePreferences } from '../hooks/preferences';

/**
 * Reconciles the locally-applied theme with the server-saved preferences once the authenticated
 * user's preferences load. Applies the server theme a single time per user (localStorage already
 * provided an instant theme on first paint), so it never clobbers in-session user changes.
 */
export function ThemeSync() {
  const { user, isAuthenticated } = useAuth();
  const { data } = usePreferences(isAuthenticated ? user?.id : undefined);
  const { applyTheme } = useTheme();
  const appliedForUser = useRef<number | null>(null);

  useEffect(() => {
    if (!user || !data) {
      return;
    }

    if (appliedForUser.current !== user.id) {
      appliedForUser.current = user.id;
      applyTheme({ mode: data.theme.mode, accent: data.theme.accent });
    }
  }, [user, data, applyTheme]);

  return null;
}
