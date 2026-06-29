import { Button, CardContent, CardHeader, Spinner } from '@heroui/react';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import type { Category } from '../../types/categories';
import type { Tag } from '../../types/tags';
import { LineItemsTable } from './LineItemsTable';
import { TableFooter } from './TableFooter';
import type { LineItem, LineItemInlinePatch } from './lineItemTypes';

interface LineItemsPanelProps {
  title: string;
  addLabel: string;
  kind: 'income' | 'expense';
  isIncome: boolean;
  currency: string;
  isLoading: boolean;
  isError: boolean;
  error: Error | null;
  items: LineItem[];
  table: {
    search: string;
    setSearch(search: string): void;
    pageItems: LineItem[];
    sortKey: string;
    sortDirection: 'asc' | 'desc';
    setSort(key: string, direction: 'asc' | 'desc'): void;
    pageSize: number;
    setPageSize(size: number): void;
    rangeStart: number;
    rangeEnd: number;
    filteredCount: number;
    page: number;
    setPage(page: number): void;
    pageCount: number;
  };
  categoriesById: Map<number, Category>;
  tagsById: Map<number, Tag>;
  onCreate(): void;
  onDetails(item: LineItem): void;
  onEdit(item: LineItem): void;
  onDelete(item: LineItem): void;
  onInlineSave(item: LineItem, patch: LineItemInlinePatch): Promise<void>;
}

/** Header and body presentation for the line-items section. */
export function LineItemsPanel({
  title,
  addLabel,
  kind,
  isIncome,
  currency,
  isLoading,
  isError,
  error,
  items,
  table,
  categoriesById,
  tagsById,
  onCreate,
  onDetails,
  onEdit,
  onDelete,
  onInlineSave
}: LineItemsPanelProps) {
  return (
    <>
      <CardHeader className="px-6 pt-6 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-lg font-semibold shrink-0">{title}</h2>
        <div className="flex items-center gap-3 w-full sm:w-auto">
          <input
            type="search"
            value={table.search}
            onChange={event => table.setSearch(event.target.value)}
            placeholder="Search name or tag…"
            aria-label={`Search ${kind}`}
            className="w-full sm:w-64 rounded-lg border border-border bg-surface px-3 py-1.5 text-sm outline-none focus-visible:ring-2 focus-visible:ring-focus"
          />
          <Button size="sm" onPress={onCreate} className="shrink-0">
            {addLabel}
          </Button>
        </div>
      </CardHeader>
      <CardContent className="px-2 pb-4 sm:px-6 sm:pb-6">
        {isLoading ? (
          <div className="flex justify-center py-10">
            <Spinner size="lg" color="accent" />
          </div>
        ) : isError ? (
          <div className="px-4">
            <ApiErrorDisplay
              error={error ?? new Error('Unknown error')}
              title={`Failed to load ${kind}`}
              showDetails={showErrorDetails}
            />
          </div>
        ) : items.length === 0 ? (
          <p className="py-8 text-center text-sm text-muted">
            No {kind} line items yet. Add one to start building this spending plan.
          </p>
        ) : (
          <>
            <LineItemsTable
              title={title}
              kind={kind}
              isIncome={isIncome}
              currency={currency}
              items={table.pageItems}
              searchTerm={table.search}
              sortKey={table.sortKey}
              sortDirection={table.sortDirection}
              onSortChange={table.setSort}
              categoriesById={categoriesById}
              tagsById={tagsById}
              onDetails={onDetails}
              onEdit={onEdit}
              onDelete={onDelete}
              onInlineSave={onInlineSave}
            />
            <TableFooter table={table} />
          </>
        )}
      </CardContent>
    </>
  );
}
