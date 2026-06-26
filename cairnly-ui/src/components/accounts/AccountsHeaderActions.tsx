import { Button } from '@heroui/react';
import { Filter, Plus, RefreshCw } from 'lucide-react';

/**
 * Stubbed top-bar actions for the Accounts page (Filters, Refresh all, Add
 * account). Presentational only — wire up handlers once an accounts API exists.
 */
export function AccountsHeaderActions() {
  return (
    <>
      <Button variant="outline" size="sm" className="hidden sm:inline-flex">
        <Filter className="size-4" />
        Filters
      </Button>
      <Button variant="outline" size="sm" className="hidden sm:inline-flex">
        <RefreshCw className="size-4" />
        Refresh all
      </Button>
      <Button size="sm">
        <Plus className="size-4" />
        Add account
      </Button>
    </>
  );
}
