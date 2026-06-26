import { useCallback, useMemo, useState } from 'react';

/** Sort direction for a {@link useClientTable} column. */
export type SortDirection = 'asc' | 'desc';

interface UseClientTableConfig<T> {
  /**
   * Returns the lowercased, concatenated text a row is matched against when
   * searching (typically name, description, and tag names). Including tag names
   * here makes the single search box also search by tag.
   */
  getSearchText(item: T): string;
  /** Returns the comparable value for a given sort column key. */
  getSortValue(item: T, key: string): string | number;
  /** The column key sorted by default. */
  initialSortKey: string;
  /** The initial sort direction. Defaults to ascending. */
  initialSortDirection?: SortDirection;
  /** The initial page size. Defaults to 10. */
  initialPageSize?: number;
}

interface UseClientTableResult<T> {
  search: string;
  setSearch(value: string): void;
  sortKey: string;
  sortDirection: SortDirection;
  /** Toggles direction when re-selecting the active column, else sorts ascending by the new column. */
  toggleSort(key: string): void;
  /** Sets the sort column and direction directly (e.g. from a table's onSortChange). */
  setSort(key: string, direction: SortDirection): void;
  page: number;
  setPage(page: number): void;
  pageSize: number;
  setPageSize(size: number): void;
  /** The rows for the current page after search + sort + pagination. */
  pageItems: T[];
  /** Total rows after search filtering (before pagination). */
  filteredCount: number;
  /** Total number of pages (at least 1). */
  pageCount: number;
  /** 1-based index of the first row on the current page (0 when empty). */
  rangeStart: number;
  /** 1-based index of the last row on the current page (0 when empty). */
  rangeEnd: number;
}

/** Compares two sort values, supporting both strings (locale-aware) and numbers. */
function compareValues(a: string | number, b: string | number): number {
  if (typeof a === 'number' && typeof b === 'number') {
    return a - b;
  }

  return String(a).localeCompare(String(b), undefined, { numeric: true, sensitivity: 'base' });
}

/**
 * Drives a fully client-side data table: case-insensitive search, single-column
 * sorting, and pagination over an in-memory array. Intended for small datasets
 * (a few hundred rows at most) that are loaded entirely on the client.
 *
 * @typeParam T The row type.
 * @param items The complete set of rows.
 * @param config Search/sort/pagination configuration.
 */
export function useClientTable<T>(items: T[], config: UseClientTableConfig<T>): UseClientTableResult<T> {
  const { getSearchText, getSortValue, initialSortKey, initialSortDirection = 'asc', initialPageSize = 10 } = config;

  const [search, setSearchValue] = useState('');
  const [sortKey, setSortKey] = useState(initialSortKey);
  const [sortDirection, setSortDirection] = useState<SortDirection>(initialSortDirection);
  const [page, setPage] = useState(0);
  const [pageSize, setPageSizeValue] = useState(initialPageSize);

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();

    if (!term) {
      return items;
    }

    return items.filter(item => getSearchText(item).includes(term));
  }, [items, search, getSearchText]);

  const sorted = useMemo(() => {
    const copy = [...filtered];
    const direction = sortDirection === 'asc' ? 1 : -1;

    copy.sort((a, b) => direction * compareValues(getSortValue(a, sortKey), getSortValue(b, sortKey)));

    return copy;
  }, [filtered, sortKey, sortDirection, getSortValue]);

  const pageCount = Math.max(1, Math.ceil(sorted.length / pageSize));
  const clampedPage = Math.min(page, pageCount - 1);
  const start = clampedPage * pageSize;
  const pageItems = useMemo(() => sorted.slice(start, start + pageSize), [sorted, start, pageSize]);

  const setSearch = useCallback((value: string) => {
    setSearchValue(value);
    setPage(0);
  }, []);

  const setPageSize = useCallback((size: number) => {
    setPageSizeValue(size);
    setPage(0);
  }, []);

  const toggleSort = useCallback((key: string) => {
    setSortKey(prevKey => {
      if (key === prevKey) {
        setSortDirection(prev => (prev === 'asc' ? 'desc' : 'asc'));
        return prevKey;
      }
      setSortDirection('asc');
      return key;
    });
    setPage(0);
  }, []);

  const setSort = useCallback((key: string, direction: SortDirection) => {
    setSortKey(key);
    setSortDirection(direction);
    setPage(0);
  }, []);

  return {
    search,
    setSearch,
    sortKey,
    sortDirection,
    toggleSort,
    setSort,
    page: clampedPage,
    setPage,
    pageSize,
    setPageSize,
    pageItems,
    filteredCount: sorted.length,
    pageCount,
    rangeStart: sorted.length === 0 ? 0 : start + 1,
    rangeEnd: Math.min(start + pageSize, sorted.length)
  };
}
