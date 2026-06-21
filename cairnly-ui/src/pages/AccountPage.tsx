import { AccountSection } from '../components/account/AccountSection';

export function AccountPage() {
  return (
    <div className="space-y-8">
      <div className="relative overflow-hidden rounded-2xl border border-border bg-surface-secondary/40 p-8 cairnly-aurora">
        <p className="text-sm font-semibold uppercase tracking-widest text-accent">Settings</p>
        <h1 className="mt-2 text-3xl font-bold tracking-tight">Account</h1>
        <p className="text-muted mt-1">Manage your account information, linked logins, and preferences.</p>
      </div>

      <AccountSection />
    </div>
  );
}
