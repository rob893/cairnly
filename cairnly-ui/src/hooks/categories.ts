import { useMemo } from 'react';
import { useInfiniteQuery } from '@tanstack/react-query';
import { categoriesApi } from '../services/categories';
import { useAuth } from './useAuth';
import type { Category } from '../types/categories';

/** Page size used when loading categories. */
const PAGE_SIZE = 200;

export const categoryQueryKeys = {
  categories: () => ['categories'] as const
} as const;

/** A category group with its child categories, for grouped pickers/lists. */
export interface CategoryGroup {
  parent: Category;
  children: Category[];
}

/**
 * Loads all categories visible to the current user (auto-fetching every page)
 * and exposes a flat list, an id → category lookup map, and a grouped view
 * (parent groups with their children). Gated on authentication.
 */
export function useCategories() {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  const query = useInfiniteQuery({
    queryKey: categoryQueryKeys.categories(),
    queryFn: ({ pageParam }) => categoriesApi.getCategories({ first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage => (lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined),
    enabled: isAuthenticated && !isAuthLoading,
    staleTime: 5 * 60 * 1000
  });

  const categories = useMemo<Category[]>(() => query.data?.pages.flatMap(page => page.nodes ?? []) ?? [], [query.data]);

  const categoriesById = useMemo(() => {
    const map = new Map<number, Category>();
    for (const category of categories) {
      map.set(category.id, category);
    }
    return map;
  }, [categories]);

  // Groups (parentId == null) and their children, ordered by group then child name.
  const groups = useMemo<CategoryGroup[]>(() => {
    const parents = categories.filter(c => c.parentId == null);
    return parents
      .map(parent => ({
        parent,
        children: categories.filter(c => c.parentId === parent.id).sort((a, b) => a.name.localeCompare(b.name))
      }))
      .sort((a, b) => a.parent.name.localeCompare(b.parent.name));
  }, [categories]);

  return { ...query, categories, categoriesById, groups };
}
