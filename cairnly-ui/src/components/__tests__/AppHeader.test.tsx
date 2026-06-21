import { describe, it, expect, vi, beforeAll, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { AppHeader } from '../AppHeader';
import { useAuth } from '../../hooks/useAuth';

const navigateMock = vi.fn();

vi.mock('react-router', async importOriginal => {
  const actual = await importOriginal<typeof import('react-router')>();
  return { ...actual, useNavigate: () => navigateMock };
});

vi.mock('../../hooks/useAuth', () => ({
  useAuth: vi.fn()
}));

/** Builds a complete auth context value, overriding only the fields under test. */
function makeAuth(overrides: Partial<ReturnType<typeof useAuth>>): ReturnType<typeof useAuth> {
  return {
    user: null,
    isAuthenticated: false,
    isLoading: false,
    login: vi.fn(() => Promise.resolve()),
    loginWithGitHub: vi.fn(() => Promise.resolve()),
    loginWithGoogle: vi.fn(() => Promise.resolve()),
    register: vi.fn(() => Promise.resolve()),
    logout: vi.fn(() => Promise.resolve()),
    checkAuth: vi.fn(() => Promise.resolve()),
    ...overrides
  };
}

describe('AppHeader', () => {
  beforeAll(() => {
    // HeroUI's popover (react-aria) relies on these browser APIs that jsdom lacks.
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
    globalThis.ResizeObserver = class {
      observe() {}
      unobserve() {}
      disconnect() {}
    };
  });

  beforeEach(() => {
    vi.mocked(useAuth).mockReset();
    navigateMock.mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('renders the welcome message, dashboard link, and profile menu when authenticated', async () => {
    const logout = vi.fn(() => Promise.resolve());
    vi.mocked(useAuth).mockReturnValue(
      makeAuth({
        isAuthenticated: true,
        user: { id: 1, userName: 'alice', email: 'alice@example.com', roles: [] },
        logout
      })
    );
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <AppHeader />
      </MemoryRouter>
    );

    expect(screen.getByText('alice')).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Dashboard' })).toBeInTheDocument();
    // Account is no longer a top-level nav link — it lives in the profile menu.
    expect(screen.queryByRole('link', { name: 'Account' })).not.toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: /profile menu/i }));

    expect(await screen.findByRole('menuitem', { name: 'Account' })).toBeInTheDocument();
    expect(screen.getByRole('menuitem', { name: 'Sign Out' })).toBeInTheDocument();

    await user.click(screen.getByRole('menuitem', { name: 'Account' }));
    expect(navigateMock).toHaveBeenCalledWith('/account');
  });

  it('signs out through the profile menu', async () => {
    const logout = vi.fn(() => Promise.resolve());
    vi.mocked(useAuth).mockReturnValue(
      makeAuth({
        isAuthenticated: true,
        user: { id: 1, userName: 'alice', email: 'alice@example.com', roles: [] },
        logout
      })
    );
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <AppHeader />
      </MemoryRouter>
    );

    await user.click(screen.getByRole('button', { name: /profile menu/i }));
    await user.click(await screen.findByRole('menuitem', { name: 'Sign Out' }));

    expect(logout).toHaveBeenCalledOnce();
  });

  it('shows sign in and sign up links when unauthenticated', () => {
    vi.mocked(useAuth).mockReturnValue(makeAuth({ isAuthenticated: false }));

    render(
      <MemoryRouter>
        <AppHeader />
      </MemoryRouter>
    );

    expect(screen.getByRole('link', { name: 'Sign In' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Sign Up' })).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /profile menu/i })).not.toBeInTheDocument();
  });
});
