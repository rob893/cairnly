import { Button } from '@heroui/react';
import { Calendar, Filter, Plus, Search } from 'lucide-react';

/**
 * Stubbed top-bar actions for the Transactions page (Search, Date, Filters, Add).
 * Presentational only — wire up handlers once a transactions API exists.
 */
export function TransactionsHeaderActions() {
  return (
    <>
      <Button variant="outline" size="sm" className="hidden md:inline-flex">
        <Search className="size-4" />
        Search
      </Button>
      <Button variant="outline" size="sm" className="hidden md:inline-flex">
        <Calendar className="size-4" />
        Date
      </Button>
      <Button variant="outline" size="sm" className="hidden sm:inline-flex">
        <Filter className="size-4" />
        Filters
      </Button>
      <Button size="sm">
        <Plus className="size-4" />
        Add
      </Button>
    </>
  );
}
