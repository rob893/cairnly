import { useInfiniteQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { transactionsApi } from '../services/transactions';
import { accountQueryKeys } from './accounts';
import { useAuth } from './useAuth';
import type {
  CreateTransactionRequest,
  TransactionQueryParameters,
  UpdateTransactionRequest
} from '../types/transactions';

/** Default page size for the transactions list endpoint. */
const PAGE_SIZE = 100;

/** Filters that scope a transactions list (excluding pagination cursors). */
export type TransactionFilters = Omit<TransactionQueryParameters, 'first' | 'after' | 'last' | 'before'>;

export const transactionQueryKeys = {
  all: () => ['transactions'] as const,
  list: (filters: TransactionFilters) => ['transactions', 'list', filters] as const
} as const;

/**
 * Lists transactions matching the given filters with cursor-based infinite
 * pagination. Gated on authentication.
 *
 * @param filters Optional filters (e.g. `accountId`). Omit to list all.
 */
export function useTransactions(filters: TransactionFilters = {}) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useInfiniteQuery({
    queryKey: transactionQueryKeys.list(filters),
    queryFn: ({ pageParam }) =>
      transactionsApi.getTransactions({ ...filters, first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage => (lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined),
    enabled: isAuthenticated && !isAuthLoading,
    staleTime: 30 * 1000
  });
}

/**
 * Lists a single account's transactions. Convenience wrapper over
 * {@link useTransactions}; disabled until the account ID is known.
 *
 * @param accountId The account ID, or `undefined` when not yet known.
 */
export function useAccountTransactions(accountId: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useInfiniteQuery({
    queryKey: transactionQueryKeys.list({ accountId }),
    queryFn: ({ pageParam }) =>
      transactionsApi.getTransactions({ accountId, first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage => (lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined),
    enabled: isAuthenticated && !isAuthLoading && typeof accountId === 'number',
    staleTime: 30 * 1000
  });
}

/**
 * Invalidates transaction lists and every account-derived query, since a
 * transaction change shifts the account's balance, net worth, and history.
 */
function invalidateAfterMutation(queryClient: ReturnType<typeof useQueryClient>): void {
  queryClient.invalidateQueries({ queryKey: transactionQueryKeys.all() });
  queryClient.invalidateQueries({ queryKey: accountQueryKeys.accounts() });
}

export function useCreateTransaction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateTransactionRequest) => transactionsApi.createTransaction(request),
    onSuccess: () => invalidateAfterMutation(queryClient)
  });
}

export function useUpdateTransaction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateTransactionRequest }) =>
      transactionsApi.updateTransaction(id, request),
    onSuccess: () => invalidateAfterMutation(queryClient)
  });
}

export function useDeleteTransaction() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => transactionsApi.deleteTransaction(id),
    onSuccess: () => invalidateAfterMutation(queryClient)
  });
}
