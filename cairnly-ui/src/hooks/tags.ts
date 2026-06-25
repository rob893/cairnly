import { useMemo } from 'react';
import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { tagsApi } from '../services/tags';
import { useAuth } from './useAuth';
import type { CreateTagRequest, Tag } from '../types/tags';

/** Page size used when loading the user's tags. */
const PAGE_SIZE = 200;

export const tagQueryKeys = {
  tags: () => ['tags'] as const
} as const;

/**
 * Loads all of the current user's tags (auto-fetching every page) and exposes a
 * flat list plus an id → tag lookup map. Gated on authentication.
 */
export function useTags() {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  const query = useInfiniteQuery({
    queryKey: tagQueryKeys.tags(),
    queryFn: ({ pageParam }) => tagsApi.getTags({ first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage => (lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined),
    enabled: isAuthenticated && !isAuthLoading,
    staleTime: 5 * 60 * 1000
  });

  const tags = useMemo<Tag[]>(() => query.data?.pages.flatMap(page => page.nodes ?? []) ?? [], [query.data]);

  const tagsById = useMemo(() => {
    const map = new Map<number, Tag>();
    for (const tag of tags) {
      map.set(tag.id, tag);
    }
    return map;
  }, [tags]);

  return { ...query, tags, tagsById };
}

/** Creates a new tag and refreshes the tag list cache. */
export function useCreateTag() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateTagRequest) => tagsApi.createTag(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: tagQueryKeys.tags() });
    }
  });
}
