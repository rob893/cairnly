import { Link, NavLink, useNavigate } from 'react-router';
import { Button } from '@heroui/react';
import { useAuth } from '../hooks/useAuth';
import { ThemeToggle } from './ThemeToggle';

function navLinkClass({ isActive }: { isActive: boolean }): string {
  return [
    'text-sm font-medium px-3 py-1.5 rounded-lg transition-colors',
    isActive ? 'text-accent bg-surface-secondary' : 'text-muted hover:text-foreground'
  ].join(' ');
}

export function AppHeader() {
  const { isAuthenticated, user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  return (
    <header className="w-full border-b border-border bg-surface/80 backdrop-blur sticky top-0 z-40">
      <div className="max-w-6xl mx-auto px-4 h-14 flex items-center justify-between gap-4">
        <Link to="/" className="font-bold text-xl text-accent no-underline shrink-0">
          Cairnly
        </Link>

        <nav className="flex items-center gap-2">
          {isAuthenticated && user ? (
            <>
              <NavLink to="/dashboard" className={navLinkClass}>
                Dashboard
              </NavLink>
              <NavLink to="/account" className={navLinkClass}>
                Account
              </NavLink>
              <ThemeToggle />
              <Button variant="danger-soft" size="sm" onPress={handleLogout}>
                Sign Out
              </Button>
            </>
          ) : (
            <>
              <ThemeToggle />
              <Link
                to="/login"
                className="text-sm font-medium text-muted hover:text-foreground transition-colors px-3 py-1.5"
              >
                Sign In
              </Link>
              <Link
                to="/register"
                className="inline-flex items-center justify-center rounded-lg bg-accent px-3 py-1.5 text-sm font-semibold text-accent-foreground hover:opacity-90 transition-opacity"
              >
                Sign Up
              </Link>
            </>
          )}
        </nav>
      </div>
    </header>
  );
}
