import { describe, it, expect, vi, beforeAll, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter } from 'react-router';
import { AppSidebar } from '../AppSidebar';
import { useAuth } from '../../hooks/useAuth';

const navigateMock = vi.fn();

vi.mock('react-router', async importOriginal => {
  const actual = await importOriginal<typeof import('react-router')>();
  return { ...actual, useNavigate: () => navigateMock };
});

vi.mock('../../hooks/useAuth', () => ({
  useAuth: vi.fn()
}));

vi.mock('../ThemeToggle', () => ({
  ThemeToggle: () => null
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

describe('AppSidebar', () => {
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

  it('renders the brand, nav links, and user name', () => {
    vi.mocked(useAuth).mockReturnValue(
      makeAuth({
        isAuthenticated: true,
        user: { id: 1, userName: 'alice', email: 'alice@example.com', roles: [] }
      })
    );

    render(
      <MemoryRouter>
        <AppSidebar isOpen={false} onClose={vi.fn()} />
      </MemoryRouter>
    );

    expect(screen.getByRole('link', { name: 'Dashboard' })).toBeInTheDocument();
    expect(screen.getByRole('link', { name: 'Budgets' })).toBeInTheDocument();
    expect(screen.getByText('alice')).toBeInTheDocument();
  });

  it('navigates to the account page from the profile menu', async () => {
    vi.mocked(useAuth).mockReturnValue(
      makeAuth({
        isAuthenticated: true,
        user: { id: 1, userName: 'alice', email: 'alice@example.com', roles: [] }
      })
    );
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <AppSidebar isOpen={false} onClose={vi.fn()} />
      </MemoryRouter>
    );

    await user.click(screen.getByRole('button', { name: /profile menu/i }));
    await user.click(await screen.findByRole('menuitem', { name: /account/i }));

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
        <AppSidebar isOpen={false} onClose={vi.fn()} />
      </MemoryRouter>
    );

    await user.click(screen.getByRole('button', { name: /profile menu/i }));
    await user.click(await screen.findByRole('menuitem', { name: /sign out/i }));

    expect(logout).toHaveBeenCalledOnce();
  });

  it('invokes onClose when the backdrop is clicked', async () => {
    const onClose = vi.fn();
    vi.mocked(useAuth).mockReturnValue(
      makeAuth({
        isAuthenticated: true,
        user: { id: 1, userName: 'alice', email: 'alice@example.com', roles: [] }
      })
    );
    const user = userEvent.setup();

    render(
      <MemoryRouter>
        <AppSidebar isOpen={true} onClose={onClose} />
      </MemoryRouter>
    );

    await user.click(screen.getByRole('button', { name: /close navigation menu/i }));
    expect(onClose).toHaveBeenCalled();
  });
});
