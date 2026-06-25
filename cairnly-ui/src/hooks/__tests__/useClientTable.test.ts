import { describe, it, expect } from 'vitest';
import { act, renderHook } from '@testing-library/react';
import { useClientTable } from '../useClientTable';

interface Row {
  id: number;
  name: string;
  amount: number;
  tags: string[];
}

const rows: Row[] = [
  { id: 1, name: 'Netflix', amount: 500, tags: ['subscription', 'streaming'] },
  { id: 2, name: 'Rent', amount: 400000, tags: ['housing'] },
  { id: 3, name: 'Hulu', amount: 500, tags: ['subscription', 'streaming'] },
  { id: 4, name: 'Groceries', amount: 60000, tags: [] }
];

const config = {
  getSearchText: (row: Row) => [row.name, ...row.tags].join(' ').toLowerCase(),
  getSortValue: (row: Row, key: string) => (key === 'amount' ? row.amount : row.name.toLowerCase()),
  initialSortKey: 'name',
  initialPageSize: 2
};

describe('useClientTable', () => {
  it('sorts by the initial key ascending and paginates', () => {
    const { result } = renderHook(() => useClientTable(rows, config));

    // 4 rows, page size 2 => 2 pages.
    expect(result.current.pageCount).toBe(2);
    expect(result.current.filteredCount).toBe(4);
    expect(result.current.pageItems.map(r => r.name)).toEqual(['Groceries', 'Hulu']);
    expect(result.current.rangeStart).toBe(1);
    expect(result.current.rangeEnd).toBe(2);
  });

  it('moves between pages', () => {
    const { result } = renderHook(() => useClientTable(rows, config));

    act(() => result.current.setPage(1));

    expect(result.current.pageItems.map(r => r.name)).toEqual(['Netflix', 'Rent']);
    expect(result.current.rangeStart).toBe(3);
    expect(result.current.rangeEnd).toBe(4);
  });

  it('searches by name and by tag, resetting to the first page', () => {
    const { result } = renderHook(() => useClientTable(rows, config));

    act(() => result.current.setPage(1));
    act(() => result.current.setSearch('subscription'));

    expect(result.current.page).toBe(0);
    expect(result.current.filteredCount).toBe(2);
    expect(result.current.pageItems.map(r => r.name)).toEqual(['Hulu', 'Netflix']);
  });

  it('toggles sort direction and switches sort column', () => {
    const { result } = renderHook(() => useClientTable(rows, config));

    // Toggle the active column (name) to descending.
    act(() => result.current.toggleSort('name'));
    expect(result.current.sortDirection).toBe('desc');
    expect(result.current.pageItems.map(r => r.name)).toEqual(['Rent', 'Netflix']);

    // Switch to amount, ascending.
    act(() => result.current.toggleSort('amount'));
    expect(result.current.sortKey).toBe('amount');
    expect(result.current.sortDirection).toBe('asc');
    expect(result.current.pageItems.map(r => r.amount)).toEqual([500, 500]);
  });

  it('changes page size and reports a single page when everything fits', () => {
    const { result } = renderHook(() => useClientTable(rows, config));

    act(() => result.current.setPageSize(100));

    expect(result.current.pageCount).toBe(1);
    expect(result.current.pageItems).toHaveLength(4);
  });
});
