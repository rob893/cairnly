import { Button } from '@heroui/react';
import { CalendarIcon, FilterIcon, PlusIcon, SearchIcon } from '../icons/NavIcons';

/**
 * Stubbed top-bar actions for the Transactions page (Search, Date, Filters, Add).
 * Presentational only — wire up handlers once a transactions API exists.
 */
export function TransactionsHeaderActions() {
  return (
    <>
      <Button variant="outline" size="sm" className="hidden md:inline-flex">
        <SearchIcon className="size-4" />
        Search
      </Button>
      <Button variant="outline" size="sm" className="hidden md:inline-flex">
        <CalendarIcon className="size-4" />
        Date
      </Button>
      <Button variant="outline" size="sm" className="hidden sm:inline-flex">
        <FilterIcon className="size-4" />
        Filters
      </Button>
      <Button size="sm">
        <PlusIcon className="size-4" />
        Add
      </Button>
    </>
  );
}
