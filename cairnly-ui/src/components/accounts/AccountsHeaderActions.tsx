import { Button } from '@heroui/react';
import { Plus, RefreshCw } from 'lucide-react';

interface AccountsHeaderActionsProps {
  onAdd(): void;
  onRefresh(): void;
  isRefreshing: boolean;
}

/**
 * Top-bar actions for the Accounts page: refresh all balances/history and add a
 * new account.
 */
export function AccountsHeaderActions({ onAdd, onRefresh, isRefreshing }: AccountsHeaderActionsProps) {
  return (
    <>
      <Button
        variant="outline"
        size="sm"
        className="hidden sm:inline-flex"
        onPress={onRefresh}
        isPending={isRefreshing}
      >
        <RefreshCw className="size-4" />
        Refresh all
      </Button>
      <Button size="sm" onPress={onAdd}>
        <Plus className="size-4" />
        Add account
      </Button>
    </>
  );
}
