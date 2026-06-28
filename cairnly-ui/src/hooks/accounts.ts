import { useInfiniteQuery, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { accountsApi } from '../services/accounts';
import { useAuth } from './useAuth';
import type {
  BalanceHistoryTimeframe,
  CreateAccountRequest,
  SetAccountBalanceRequest,
  UpdateAccountRequest
} from '../types/accounts';

/** Default page size for the accounts list endpoint. */
const PAGE_SIZE = 100;

export const accountQueryKeys = {
  accounts: () => ['accounts'] as const,
  account: (id: number) => ['accounts', id] as const,
  netWorth: (timeframe: BalanceHistoryTimeframe) => ['accounts', 'net-worth', timeframe] as const,
  history: (timeframe: BalanceHistoryTimeframe) => ['accounts', 'history', timeframe] as const
} as const;

/**
 * Lists the current user's accounts with cursor-based infinite pagination. Gated
 * on authentication.
 */
export function useAccounts() {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useInfiniteQuery({
    queryKey: accountQueryKeys.accounts(),
    queryFn: ({ pageParam }) => accountsApi.getAccounts({ first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage => (lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined),
    enabled: isAuthenticated && !isAuthLoading,
    staleTime: 30 * 1000
  });
}

/**
 * Fetches a single account by ID. Gated on authentication.
 *
 * @param id The account ID, or `undefined` when not yet known.
 */
export function useAccount(id: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: accountQueryKeys.account(id ?? 0),
    queryFn: () => accountsApi.getAccount(id as number),
    enabled: isAuthenticated && !isAuthLoading && typeof id === 'number',
    staleTime: 30 * 1000
  });
}

/**
 * Fetches the current user's net-worth history for the selected window. Gated on
 * authentication.
 *
 * @param timeframe The window to fetch history for.
 */
export function useNetWorthHistory(timeframe: BalanceHistoryTimeframe) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: accountQueryKeys.netWorth(timeframe),
    queryFn: () => accountsApi.getNetWorthHistory(timeframe),
    enabled: isAuthenticated && !isAuthLoading,
    staleTime: 30 * 1000
  });
}

/**
 * Fetches per-account balance history for the selected window. Gated on
 * authentication.
 *
 * @param timeframe The window to fetch history for.
 */
export function useAccountHistory(timeframe: BalanceHistoryTimeframe) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: accountQueryKeys.history(timeframe),
    queryFn: () => accountsApi.getAccountHistory(timeframe),
    enabled: isAuthenticated && !isAuthLoading,
    staleTime: 30 * 1000
  });
}

/**
 * Invalidates every account-derived query (list, net worth, per-account history)
 * so balances and charts refresh after a mutation.
 */
function invalidateAccountQueries(queryClient: ReturnType<typeof useQueryClient>): void {
  queryClient.invalidateQueries({ queryKey: accountQueryKeys.accounts() });
}

export function useCreateAccount() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateAccountRequest) => accountsApi.createAccount(request),
    onSuccess: () => invalidateAccountQueries(queryClient)
  });
}

export function useUpdateAccount() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: UpdateAccountRequest }) =>
      accountsApi.updateAccount(id, request),
    onSuccess: () => invalidateAccountQueries(queryClient)
  });
}

export function useDeleteAccount() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => accountsApi.deleteAccount(id),
    onSuccess: () => invalidateAccountQueries(queryClient)
  });
}

export function useSetAccountBalance() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, request }: { id: number; request: SetAccountBalanceRequest }) =>
      accountsApi.setBalance(id, request),
    onSuccess: () => {
      invalidateAccountQueries(queryClient);
      // Setting the balance records an adjustment transaction. Use a literal key to avoid a
      // circular import with hooks/transactions (which imports accountQueryKeys).
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
    }
  });
}
