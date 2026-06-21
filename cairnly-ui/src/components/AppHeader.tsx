import type { Key } from 'react';
import { Link, NavLink, useNavigate } from 'react-router';
import { Avatar, Dropdown } from '@heroui/react';
import { useAuth } from '../hooks/useAuth';
import { CairnMark } from './CairnMark';

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

  const handleMenuAction = (key: Key) => {
    if (key === 'account') {
      navigate('/account');
    } else if (key === 'signout') {
      void handleLogout();
    }
  };

  const avatarInitial = user?.userName?.[0]?.toUpperCase() ?? 'U';

  return (
    <header className="w-full border-b border-border bg-surface/80 backdrop-blur sticky top-0 z-40">
      <div className="max-w-[2000px] mx-auto px-6 lg:px-10 h-14 flex items-center justify-between gap-4">
        <Link to="/" className="flex items-center gap-2 font-bold text-xl text-foreground no-underline shrink-0">
          <CairnMark className="size-6 text-accent" />
          Cairnly
        </Link>

        <nav className="flex items-center gap-3">
          {isAuthenticated && user ? (
            <>
              <div className="hidden sm:flex items-center gap-1">
                <NavLink to="/dashboard" className={navLinkClass}>
                  Dashboard
                </NavLink>
              </div>

              <div className="hidden sm:block h-6 w-px bg-border" aria-hidden="true" />

              <span className="hidden lg:inline text-sm text-muted">
                Welcome, <span className="text-foreground font-medium">{user.userName}</span>
              </span>

              <Dropdown>
                <Dropdown.Trigger
                  aria-label="Open profile menu"
                  className="rounded-full outline-none transition-opacity hover:opacity-90 focus-visible:ring-2 focus-visible:ring-focus focus-visible:ring-offset-2 focus-visible:ring-offset-surface"
                >
                  <Avatar color="accent" size="md" className="cursor-pointer">
                    <Avatar.Fallback>{avatarInitial}</Avatar.Fallback>
                  </Avatar>
                </Dropdown.Trigger>
                <Dropdown.Popover placement="bottom end" className="min-w-44">
                  <Dropdown.Menu aria-label="Profile actions" onAction={handleMenuAction}>
                    <Dropdown.Item id="account">Account</Dropdown.Item>
                    <Dropdown.Item id="signout" className="text-danger">
                      Sign Out
                    </Dropdown.Item>
                  </Dropdown.Menu>
                </Dropdown.Popover>
              </Dropdown>
            </>
          ) : (
            <>
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
