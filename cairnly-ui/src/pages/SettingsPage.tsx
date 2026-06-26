import { useMemo, useState } from 'react';
import { Separator } from '@heroui/react';
import { ProfileSection } from '../components/settings/ProfileSection';
import { PreferencesSection } from '../components/settings/PreferencesSection';
import { SecuritySection } from '../components/settings/SecuritySection';
import { SettingsStubSection } from '../components/settings/SettingsStubSection';
import { usePageHeader } from '../hooks/usePageHeader';

type SettingsSectionId = 'profile' | 'preferences' | 'security' | 'categories' | 'rules' | 'tags';

interface NavGroup {
  label: string;
  items: ReadonlyArray<{ id: SettingsSectionId; label: string }>;
}

const navGroups: ReadonlyArray<NavGroup> = [
  {
    label: 'Account',
    items: [
      { id: 'profile', label: 'Profile' },
      { id: 'preferences', label: 'Preferences' },
      { id: 'security', label: 'Security' }
    ]
  },
  {
    label: 'General',
    items: [
      { id: 'categories', label: 'Categories' },
      { id: 'rules', label: 'Rules' },
      { id: 'tags', label: 'Tags' }
    ]
  }
];

const settingsHeader = { title: 'Settings' };

export function SettingsPage() {
  usePageHeader(settingsHeader);
  const [active, setActive] = useState<SettingsSectionId>('profile');

  const content = useMemo(() => {
    switch (active) {
      case 'profile':
        return <ProfileSection />;
      case 'preferences':
        return <PreferencesSection />;
      case 'security':
        return <SecuritySection />;
      case 'categories':
        return (
          <SettingsStubSection
            title="Categories"
            description="Organize transactions into custom categories and groups. This is coming soon."
          />
        );
      case 'rules':
        return (
          <SettingsStubSection
            title="Rules"
            description="Automatically categorize and tag transactions with rules. This is coming soon."
          />
        );
      case 'tags':
        return (
          <SettingsStubSection
            title="Tags"
            description="Create and manage tags to label transactions across categories. This is coming soon."
          />
        );
      default:
        return null;
    }
  }, [active]);

  return (
    <div className="grid grid-cols-1 gap-6 lg:grid-cols-[325px_1fr] lg:items-start">
      <nav className="space-y-6 rounded-2xl border border-border bg-surface p-3">
        {navGroups.map(group => (
          <div key={group.label}>
            <p className="px-3 pb-1 pt-1 text-base font-semibold text-foreground">{group.label}</p>
            <Separator className="my-2" />
            <ul className="space-y-0.5">
              {group.items.map(item => {
                const isActive = item.id === active;
                return (
                  <li key={item.id}>
                    <button
                      type="button"
                      onClick={() => setActive(item.id)}
                      aria-current={isActive ? 'page' : undefined}
                      className={[
                        'w-full rounded-lg px-3 py-2.5 text-left text-base font-medium transition-colors',
                        isActive
                          ? 'bg-accent text-accent-foreground'
                          : 'text-muted hover:bg-surface-secondary hover:text-foreground'
                      ].join(' ')}
                    >
                      {item.label}
                    </button>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>

      <div className="min-w-0">{content}</div>
    </div>
  );
}
