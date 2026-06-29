import { useMemo } from 'react';
import { cadenceBreakdown, periodsPerYear } from '../../utils/cadence';
import { useClientTable } from '../../hooks/useClientTable';
import type { Category } from '../../types/categories';
import type { Tag } from '../../types/tags';
import type { LineItem } from './lineItemTypes';

/** Creates the client-side search/sort/pagination model for spending-plan line items. */
export function useLineItemsClientTable(
  items: LineItem[],
  categoriesById: Map<number, Category>,
  tagsById: Map<number, Tag>
) {
  const categoryNameOf = useMemo(
    () =>
      (item: LineItem): string =>
        categoriesById.get(item.categoryId)?.name ?? '',
    [categoriesById]
  );
  const tagNamesOf = useMemo(
    () =>
      (item: LineItem): string[] =>
        item.tagIds.map(id => tagsById.get(id)?.name ?? '').filter(name => name !== ''),
    [tagsById]
  );
  const getSearchText = useMemo(
    () => (item: LineItem) =>
      [item.name, item.description ?? '', categoryNameOf(item), ...tagNamesOf(item)].join(' ').toLowerCase(),
    [tagNamesOf, categoryNameOf]
  );
  const getSortValue = useMemo(
    () =>
      (item: LineItem, key: string): string | number => {
        switch (key) {
          case 'name':
            return item.name.toLowerCase();
          case 'category':
            return categoryNameOf(item).toLowerCase();
          case 'amount':
            return item.amount;
          case 'cadence':
            return periodsPerYear(item.cadence);
          case 'daily':
          case 'weekly':
          case 'monthly':
          case 'annual':
            return cadenceBreakdown(item.amount, item.cadence)[key];
          default:
            return item.name.toLowerCase();
        }
      },
    [categoryNameOf]
  );

  return {
    table: useClientTable(items, { getSearchText, getSortValue, initialSortKey: 'name', initialPageSize: 10 }),
    tagNamesOf
  };
}
