import { Button } from '@heroui/react';
import { Moon, Sun } from 'lucide-react';
import { useTheme } from '../hooks/useTheme';

/**
 * A compact button that toggles between light and dark color schemes.
 */
export function ThemeToggle() {
  const { isDark, setMode } = useTheme();

  return (
    <Button
      variant="ghost"
      size="sm"
      aria-label={isDark ? 'Switch to light mode' : 'Switch to dark mode'}
      onPress={() => setMode(isDark ? 'light' : 'dark')}
    >
      {isDark ? <Sun className="size-[18px]" /> : <Moon className="size-[18px]" />}
    </Button>
  );
}
