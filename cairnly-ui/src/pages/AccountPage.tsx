import { AccountSection } from '../components/account/AccountSection';
import { usePageHeader } from '../hooks/usePageHeader';

const accountHeader = { title: 'Account' };

export function AccountPage() {
  usePageHeader(accountHeader);

  return <AccountSection />;
}
