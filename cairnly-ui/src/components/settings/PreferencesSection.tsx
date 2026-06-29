import { Card, CardContent, CardHeader, ToggleButton, ToggleButtonGroup } from '@heroui/react';
import { useAuth } from '../../hooks/useAuth';
import { useTheme } from '../../hooks/useTheme';
import { useUpdatePreferences } from '../../hooks/preferences';
import { ACCENT_PRESETS, type ThemeMode } from '../../constants/theme';
import { showSuccessToast } from '../../utils/notifications';

/**
 * The Preferences settings section: appearance controls (color scheme and accent
 * color), persisted to the user's saved preferences.
 */
export function PreferencesSection() {
  const { user } = useAuth();
  const { mode, accent, setMode, setAccent } = useTheme();
  const updatePreferences = useUpdatePreferences(user?.id ?? 0);

  const persist = async (next: { mode: ThemeMode; accent: string }) => {
    if (user) {
      await updatePreferences.mutateAsync({ theme: next });
      showSuccessToast('Preferences updated');
    }
  };

  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6">
        <h2 className="text-lg font-semibold">Appearance</h2>
      </CardHeader>
      <CardContent className="px-6 pb-6 space-y-8">
        {/* Color scheme */}
        <div>
          <p className="text-sm font-semibold mb-1">Color scheme</p>
          <p className="text-sm text-muted mb-3">Choose how Cairnly looks. “System” follows your device.</p>
          <ToggleButtonGroup
            selectionMode="single"
            disallowEmptySelection
            selectedKeys={new Set([mode])}
            onSelectionChange={keys => {
              const next = [...keys][0] as ThemeMode | undefined;
              if (next) {
                setMode(next);
                void persist({ mode: next, accent });
              }
            }}
            aria-label="Color scheme"
          >
            <ToggleButton id="system">System</ToggleButton>
            <ToggleButton id="light">Light</ToggleButton>
            <ToggleButton id="dark">Dark</ToggleButton>
          </ToggleButtonGroup>
        </div>

        {/* Accent color */}
        <div>
          <p className="text-sm font-semibold mb-1">Accent color</p>
          <p className="text-sm text-muted mb-3">Personalize the highlight color used across the app.</p>
          <ToggleButtonGroup
            selectionMode="single"
            disallowEmptySelection
            selectedKeys={new Set([accent])}
            onSelectionChange={keys => {
              const next = [...keys][0];
              if (next != null) {
                setAccent(String(next));
                void persist({ mode, accent: String(next) });
              }
            }}
            aria-label="Accent color"
          >
            {ACCENT_PRESETS.map(preset => (
              <ToggleButton key={preset.id} id={preset.id} isIconOnly aria-label={preset.label}>
                <span className="size-4 rounded-full" style={{ backgroundColor: preset.accent }} />
              </ToggleButton>
            ))}
          </ToggleButtonGroup>
        </div>
      </CardContent>
    </Card>
  );
}
