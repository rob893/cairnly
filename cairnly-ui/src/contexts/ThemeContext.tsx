import { createContext, useCallback, useEffect, useLayoutEffect, useMemo, useState, type ReactNode } from 'react';
import { ACCENT_PRESETS, DEFAULT_ACCENT_ID, DEFAULT_MODE, getAccentPreset, type ThemeMode } from '../constants/theme';

const STORAGE_KEY = 'cairnly_theme';

export interface ThemeState {
  mode: ThemeMode;
  accent: string;
}

export interface ThemeContextType extends ThemeState {
  /** Whether the dark color scheme is currently active (after resolving `system`). */
  isDark: boolean;
  setMode(mode: ThemeMode): void;
  setAccent(accent: string): void;
  /** Applies a theme without persisting to the backend (used to hydrate from the server). */
  applyTheme(next: Partial<ThemeState>): void;
}

const ThemeContext = createContext<ThemeContextType | undefined>(undefined);

export { ThemeContext };

function readStoredTheme(): ThemeState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (raw) {
      const parsed = JSON.parse(raw) as Partial<ThemeState>;
      const mode =
        parsed.mode === 'light' || parsed.mode === 'dark' || parsed.mode === 'system' ? parsed.mode : DEFAULT_MODE;
      const accent = ACCENT_PRESETS.some(p => p.id === parsed.accent) ? (parsed.accent as string) : DEFAULT_ACCENT_ID;
      return { mode, accent };
    }
  } catch {
    // Ignore malformed/unavailable storage and fall back to defaults.
  }
  return { mode: DEFAULT_MODE, accent: DEFAULT_ACCENT_ID };
}

function prefersDark(): boolean {
  return typeof window !== 'undefined' && window.matchMedia('(prefers-color-scheme: dark)').matches;
}

function resolveIsDark(mode: ThemeMode): boolean {
  return mode === 'dark' || (mode === 'system' && prefersDark());
}

function applyToDocument(state: ThemeState): void {
  const root = document.documentElement;
  const isDark = resolveIsDark(state.mode);

  root.classList.toggle('dark', isDark);
  root.setAttribute('data-theme', isDark ? 'dark' : 'light');

  const preset = getAccentPreset(state.accent);
  root.style.setProperty('--accent', preset.accent);
  root.style.setProperty('--accent-foreground', preset.accentForeground);
  root.style.setProperty('--focus', preset.accent);
}

export function ThemeProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<ThemeState>(() => readStoredTheme());
  const [isDark, setIsDark] = useState<boolean>(() => resolveIsDark(state.mode));

  // Apply + persist whenever the theme changes (before paint to avoid flashes).
  useLayoutEffect(() => {
    applyToDocument(state);
    setIsDark(resolveIsDark(state.mode));
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } catch {
      // Storage may be unavailable (private mode); theme still applies for the session.
    }
  }, [state]);

  // Re-resolve when the OS color scheme changes and the user is on `system`.
  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const handler = () => {
      if (state.mode === 'system') {
        applyToDocument({ mode: 'system', accent: state.accent });
        setIsDark(prefersDark());
      }
    };
    media.addEventListener('change', handler);
    return () => media.removeEventListener('change', handler);
  }, [state.mode, state.accent]);

  const setMode = useCallback((mode: ThemeMode) => setState(prev => ({ ...prev, mode })), []);
  const setAccent = useCallback((accent: string) => setState(prev => ({ ...prev, accent })), []);
  const applyTheme = useCallback((next: Partial<ThemeState>) => setState(prev => ({ ...prev, ...next })), []);

  const value = useMemo<ThemeContextType>(
    () => ({ ...state, isDark, setMode, setAccent, applyTheme }),
    [state, isDark, setMode, setAccent, applyTheme]
  );

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}
