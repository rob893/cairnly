import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AppHeader } from '../AppHeader';
import { useAuth } from '../../hooks/useAuth';

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
  beforeEach(() => {
    vi.mocked(useAuth).mockReset();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it('greets the signed-in user by name', () => {
    vi.mocked(useAuth).mockReturnValue(
      makeAuth({
        isAuthenticated: true,
        user: { id: 1, userName: 'alice', email: 'alice@example.com', roles: [] }
      })
    );

    render(<AppHeader onMenuClick={vi.fn()} />);

    expect(screen.getByRole('heading', { name: /alice!/ })).toBeInTheDocument();
  });

  it('invokes onMenuClick when the menu button is pressed', async () => {
    const onMenuClick = vi.fn();
    vi.mocked(useAuth).mockReturnValue(
      makeAuth({
        isAuthenticated: true,
        user: { id: 1, userName: 'alice', email: 'alice@example.com', roles: [] }
      })
    );
    const user = userEvent.setup();

    render(<AppHeader onMenuClick={onMenuClick} />);

    await user.click(screen.getByRole('button', { name: /open navigation menu/i }));
    expect(onMenuClick).toHaveBeenCalledOnce();
  });
});
