import { useMemo, useState } from 'react';
import { Button } from '@heroui/react';
import { SelectField } from '../components/SelectField';
import { TransactionsHeaderActions } from '../components/transactions/TransactionsHeaderActions';
import { TransactionsList } from '../components/transactions/TransactionsList';
import { usePageHeader } from '../hooks/usePageHeader';

type TransactionsTab = 'all' | 'receipts';

const tabs: ReadonlyArray<{ id: TransactionsTab; label: string }> = [
  { id: 'all', label: 'All' },
  { id: 'receipts', label: 'Receipts' }
];

const filterOptions = [{ value: 'all', label: 'All transactions' }];

/**
 * The Transactions page: a tabbed top-bar title, a toolbar, and a grouped list of
 * placeholder transactions. There is no transactions API yet.
 */
export function TransactionsPage() {
  const [tab, setTab] = useState<TransactionsTab>('all');
  const [filter, setFilter] = useState('all');

  const header = useMemo(
    () => ({
      title: (
        <div className="flex items-center gap-5">
          <span>Transactions</span>
          <nav className="flex items-center gap-4 text-sm font-medium">
            {tabs.map(({ id, label }) => (
              <button
                key={id}
                type="button"
                onClick={() => setTab(id)}
                className={[
                  'border-b-2 pb-0.5 transition-colors',
                  tab === id ? 'border-accent text-foreground' : 'border-transparent text-muted hover:text-foreground'
                ].join(' ')}
              >
                {label}
              </button>
            ))}
          </nav>
        </div>
      ),
      actions: <TransactionsHeaderActions />
    }),
    [tab]
  );

  usePageHeader(header);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="w-48">
          <SelectField aria-label="Transaction filter" value={filter} onChange={setFilter} options={filterOptions} />
        </div>
        <div className="flex items-center gap-2">
          <Button variant="ghost" size="sm">
            Edit multiple
          </Button>
          <Button variant="ghost" size="sm">
            Sort
          </Button>
          <Button variant="ghost" size="sm">
            Columns
          </Button>
        </div>
      </div>

      <TransactionsList />
    </div>
  );
}
