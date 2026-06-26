import { useState, type Key } from 'react';
import { Link, NavLink, useNavigate } from 'react-router';
import { Avatar, Dropdown } from '@heroui/react';
import {
  ArrowLeftRight,
  ChartColumn,
  ChevronDown,
  ChevronUp,
  House,
  Landmark,
  Layers,
  LogOut,
  Settings,
  type LucideIcon
} from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { CairnMark } from './CairnMark';

interface NavItem {
  to: string;
  label: string;
  Icon: LucideIcon;
}

const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', Icon: House },
  { to: '/accounts', label: 'Accounts', Icon: Landmark },
  { to: '/transactions', label: 'Transactions', Icon: ArrowLeftRight },
  { to: '/cash-flow', label: 'Cash Flow', Icon: ChartColumn },
  { to: '/spending-plans', label: 'Spending Plans', Icon: Layers }
];

function navLinkClass({ isActive }: { isActive: boolean }): string {
  return [
    'flex items-center gap-3 rounded-lg px-3 py-2.5 text-base font-medium transition-colors no-underline',
    isActive ? 'bg-surface-secondary text-foreground' : 'text-muted hover:bg-surface-secondary/60 hover:text-foreground'
  ].join(' ');
}

interface AppSidebarProps {
  /** Whether the mobile drawer is open. Ignored at `lg` and up (always visible). */
  isOpen: boolean;
  /** Invoked to close the mobile drawer (backdrop click or nav selection). */
  onClose: () => void;
}

/**
 * The application's primary left navigation rail: brand, page links, and a
 * footer with the user/settings menu. Renders as a fixed sidebar at `lg` and up,
 * and as a slide-in drawer with a backdrop on smaller screens.
 */
export function AppSidebar({ isOpen, onClose }: AppSidebarProps) {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);

  const handleLogout = async () => {
    await logout();
    navigate('/login', { replace: true });
  };

  const handleMenuAction = (key: Key) => {
    if (key === 'settings') {
      navigate('/settings');
    } else if (key === 'signout') {
      void handleLogout();
    }
  };

  const avatarInitial = user?.userName?.[0]?.toUpperCase() ?? 'U';

  return (
    <>
      {isOpen && (
        <button
          type="button"
          aria-label="Close navigation menu"
          className="fixed inset-0 z-40 bg-black/50 backdrop-blur-sm lg:hidden"
          onClick={onClose}
        />
      )}

      <aside
        className={[
          'fixed inset-y-0 left-0 z-50 flex w-64 flex-col border-r border-border bg-surface',
          'transition-transform duration-200 ease-out lg:translate-x-0',
          isOpen ? 'translate-x-0' : '-translate-x-full'
        ].join(' ')}
      >
        <div className="flex h-14 items-center px-5">
          <Link
            to="/dashboard"
            className="flex items-center gap-2 text-lg font-bold text-foreground no-underline"
            onClick={onClose}
          >
            <CairnMark className="size-6 text-accent" />
            Cairnly
          </Link>
        </div>

        <nav className="flex-1 space-y-1 overflow-y-auto px-3 py-2">
          {navItems.map(({ to, label, Icon }) => (
            <NavLink key={to} to={to} className={navLinkClass} onClick={onClose}>
              <Icon className="size-5 shrink-0" />
              {label}
            </NavLink>
          ))}
        </nav>

        <div className="border-t border-border p-3">
          {user && (
            <Dropdown isOpen={menuOpen} onOpenChange={setMenuOpen}>
              <Dropdown.Trigger
                aria-label="Open profile menu"
                className="flex w-full items-center gap-3 rounded-lg px-2 py-2 text-left outline-none transition-colors hover:bg-surface-secondary focus-visible:ring-2 focus-visible:ring-focus"
              >
                <Avatar color="accent" size="sm">
                  <Avatar.Fallback>{avatarInitial}</Avatar.Fallback>
                </Avatar>
                <span className="min-w-0 flex-1 truncate text-sm font-medium text-foreground">{user.userName}</span>
                {menuOpen ? (
                  <ChevronUp className="size-4 shrink-0 text-muted" />
                ) : (
                  <ChevronDown className="size-4 shrink-0 text-muted" />
                )}
              </Dropdown.Trigger>
              <Dropdown.Popover placement="top start" className="min-w-56">
                <Dropdown.Menu aria-label="Profile actions" onAction={handleMenuAction}>
                  <Dropdown.Item id="settings">
                    <span className="flex items-center gap-2">
                      <Settings className="size-4" />
                      Settings
                    </span>
                  </Dropdown.Item>
                  <Dropdown.Item id="signout" className="text-danger">
                    <span className="flex items-center gap-2">
                      <LogOut className="size-4" />
                      Sign Out
                    </span>
                  </Dropdown.Item>
                </Dropdown.Menu>
              </Dropdown.Popover>
            </Dropdown>
          )}
        </div>
      </aside>
    </>
  );
}
