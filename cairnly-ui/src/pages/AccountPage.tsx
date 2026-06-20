import { Tabs } from '@heroui/react';
import { AccountSection } from '../components/account/AccountSection';
import { PreferencesSection } from '../components/account/PreferencesSection';

export function AccountPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold">Account Settings</h1>
        <p className="text-muted mt-1">Manage your account information and settings.</p>
      </div>

      <Tabs defaultSelectedKey="account" orientation="vertical" className="flex flex-col md:flex-row gap-6">
        <Tabs.ListContainer className="md:w-56 shrink-0">
          <Tabs.List aria-label="Account settings sections" className="md:flex-col md:items-stretch">
            <Tabs.Tab id="account">
              Account
              <Tabs.Indicator />
            </Tabs.Tab>
            <Tabs.Tab id="preferences">
              Preferences
              <Tabs.Indicator />
            </Tabs.Tab>
          </Tabs.List>
        </Tabs.ListContainer>

        <Tabs.Panel id="account" className="flex-1 min-w-0">
          <AccountSection />
        </Tabs.Panel>
        <Tabs.Panel id="preferences" className="flex-1 min-w-0">
          <PreferencesSection />
        </Tabs.Panel>
      </Tabs>
    </div>
  );
}
