import { beforeAll, beforeEach, describe, expect, it } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { ThemeProvider } from '../ThemeContext';
import { useTheme } from '../../hooks/useTheme';
import { getAccentPreset } from '../../constants/theme';

function Consumer() {
  const { mode, accent, setMode, setAccent } = useTheme();
  return (
    <div>
      <span data-testid="mode">{mode}</span>
      <span data-testid="accent">{accent}</span>
      <button onClick={() => setMode('dark')}>set-dark</button>
      <button onClick={() => setMode('light')}>set-light</button>
      <button onClick={() => setAccent('blue')}>set-blue</button>
    </div>
  );
}

describe('ThemeProvider', () => {
  beforeAll(() => {
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: (query: string) => ({
        matches: false,
        media: query,
        addEventListener: () => {},
        removeEventListener: () => {},
        addListener: () => {},
        removeListener: () => {},
        dispatchEvent: () => false,
        onchange: null
      })
    });
  });

  beforeEach(() => {
    localStorage.clear();
    document.documentElement.className = '';
    document.documentElement.removeAttribute('style');
    document.documentElement.removeAttribute('data-theme');
  });

  it('defaults to dark mode and the green accent', () => {
    render(
      <ThemeProvider>
        <Consumer />
      </ThemeProvider>
    );

    expect(screen.getByTestId('mode').textContent).toBe('dark');
    expect(screen.getByTestId('accent').textContent).toBe('green');
    expect(document.documentElement.style.getPropertyValue('--accent')).toBe(getAccentPreset('green').accent);
  });

  it('applies the dark class and persists when switching to dark mode', async () => {
    const user = userEvent.setup();
    render(
      <ThemeProvider>
        <Consumer />
      </ThemeProvider>
    );

    await user.click(screen.getByText('set-dark'));

    expect(document.documentElement.classList.contains('dark')).toBe(true);
    expect(document.documentElement.getAttribute('data-theme')).toBe('dark');
    expect(JSON.parse(localStorage.getItem('cairnly_theme') ?? '{}').mode).toBe('dark');

    await user.click(screen.getByText('set-light'));
    expect(document.documentElement.classList.contains('dark')).toBe(false);
  });

  it('overrides the accent CSS variable when the accent changes', async () => {
    const user = userEvent.setup();
    render(
      <ThemeProvider>
        <Consumer />
      </ThemeProvider>
    );

    await user.click(screen.getByText('set-blue'));

    expect(screen.getByTestId('accent').textContent).toBe('blue');
    expect(document.documentElement.style.getPropertyValue('--accent')).toBe(getAccentPreset('blue').accent);
    expect(JSON.parse(localStorage.getItem('cairnly_theme') ?? '{}').accent).toBe('blue');
  });
});
