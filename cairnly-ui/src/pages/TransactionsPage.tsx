import { useMemo, useRef } from 'react';
import { Button } from '@heroui/react';
import { Plus } from 'lucide-react';
import { TransactionsTable, type TransactionsTableHandle } from '../components/transactions/TransactionsTable';
import { usePageHeader } from '../hooks/usePageHeader';

/**
 * The Transactions page: a reusable, inline-editable table of all of the current
 * user's transactions across every account. The add action lives in the top bar.
 */
export function TransactionsPage() {
  const tableRef = useRef<TransactionsTableHandle>(null);

  const header = useMemo(
    () => ({
      title: 'Transactions',
      actions: (
        <Button size="sm" onPress={() => tableRef.current?.openCreate()}>
          <Plus className="size-4" />
          Add transaction
        </Button>
      )
    }),
    []
  );

  usePageHeader(header);

  return (
    <div className="space-y-4">
      <TransactionsTable ref={tableRef} showAccount showToolbar={false} />
    </div>
  );
}
