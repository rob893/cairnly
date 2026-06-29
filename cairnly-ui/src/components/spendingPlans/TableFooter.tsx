import { Button } from '@heroui/react';
import { SelectField } from '../SelectField';

interface TableFooterProps {
  table: {
    pageSize: number;
    setPageSize(size: number): void;
    rangeStart: number;
    rangeEnd: number;
    filteredCount: number;
    page: number;
    setPage(page: number): void;
    pageCount: number;
  };
}

const PAGE_SIZE_OPTIONS = [
  { value: '10', label: '10' },
  { value: '25', label: '25' },
  { value: '50', label: '50' },
  { value: '100', label: '100' }
];

/** Pagination footer for client-side line item tables. */
export function TableFooter({ table }: TableFooterProps) {
  return (
    <div className="flex flex-col gap-3 px-3 pt-4 sm:flex-row sm:items-center sm:justify-end">
      <div className="flex items-center gap-2">
        <span className="text-sm text-muted">Rows per page:</span>
        <SelectField
          aria-label="Rows per page"
          value={String(table.pageSize)}
          onChange={value => table.setPageSize(Number(value))}
          options={PAGE_SIZE_OPTIONS}
          className="w-20"
        />
      </div>
      <div className="flex items-center gap-3">
        <span className="text-sm text-muted tabular-nums">
          {table.rangeStart}-{table.rangeEnd} of {table.filteredCount}
        </span>
        <div className="flex items-center gap-1">
          <Button
            variant="ghost"
            size="sm"
            aria-label="Previous page"
            isDisabled={table.page === 0}
            onPress={() => table.setPage(table.page - 1)}
          >
            ‹
          </Button>
          <Button
            variant="ghost"
            size="sm"
            aria-label="Next page"
            isDisabled={table.page >= table.pageCount - 1}
            onPress={() => table.setPage(table.page + 1)}
          >
            ›
          </Button>
        </div>
      </div>
    </div>
  );
}
