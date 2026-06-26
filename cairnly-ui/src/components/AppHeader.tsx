import { Button } from '@heroui/react';
import { useAuth } from '../hooks/useAuth';
import { usePageHeaderState } from '../hooks/usePageHeader';
import { Menu } from 'lucide-react';

/** Returns a time-of-day greeting ("Good morning/afternoon/evening"). */
function greetingForHour(hour: number): string {
  if (hour < 12) {
    return 'Good morning';
  }

  if (hour < 18) {
    return 'Good afternoon';
  }

  return 'Good evening';
}

interface AppHeaderProps {
  /** Opens the mobile navigation drawer. Only shown below the `lg` breakpoint. */
  onMenuClick: () => void;
}

/**
 * The slim top bar above page content: a mobile menu trigger, a title (a
 * page-supplied title from {@link usePageHeader}, or a time-of-day greeting by
 * default), and optional page actions. Primary navigation and the profile menu
 * live in {@link AppSidebar}.
 */
export function AppHeader({ onMenuClick }: AppHeaderProps) {
  const { user } = useAuth();
  const { title, actions } = usePageHeaderState();
  const greeting = greetingForHour(new Date().getHours());
  const name = user?.userName ?? 'there';
  const heading = title ?? `${greeting}, ${name}!`;

  return (
    <header className="sticky top-0 z-30 h-14 border-b border-border bg-background/80 backdrop-blur">
      <div className="flex h-full items-center gap-3 px-4 sm:px-6 lg:px-8">
        <Button
          variant="ghost"
          size="sm"
          aria-label="Open navigation menu"
          className="lg:hidden"
          onPress={onMenuClick}
        >
          <Menu className="size-5" />
        </Button>

        <h1 className="truncate text-base font-semibold text-foreground sm:text-lg">{heading}</h1>

        {actions && <div className="ml-auto flex items-center gap-2">{actions}</div>}
      </div>
    </header>
  );
}
