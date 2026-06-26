import { useInfiniteQuery, useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { spendingPlanExpensesApi, spendingPlanIncomesApi, spendingPlansApi } from '../services/spendingPlans';
import { useAuth } from './useAuth';
import type {
  SpendingPlanSummary,
  CreateSpendingPlanExpenseRequest,
  CreateSpendingPlanIncomeRequest,
  CreateSpendingPlanRequest,
  UpdateSpendingPlanExpenseRequest,
  UpdateSpendingPlanIncomeRequest,
  UpdateSpendingPlanRequest
} from '../types/spendingPlans';

/** Default page size for spendingPlan list endpoints. */
const PAGE_SIZE = 100;

export const spendingPlanQueryKeys = {
  spendingPlans: () => ['spendingPlans'] as const,
  spendingPlan: (id: number) => ['spendingPlans', id] as const,
  summary: (id: number) => ['spendingPlans', id, 'summary'] as const,
  incomes: (spendingPlanId: number) => ['spendingPlans', spendingPlanId, 'income'] as const,
  expenses: (spendingPlanId: number) => ['spendingPlans', spendingPlanId, 'expenses'] as const
} as const;

/**
 * Lists the current user's spendingPlans with cursor-based infinite pagination. Gated
 * on authentication.
 */
export function useSpendingPlans() {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useInfiniteQuery({
    queryKey: spendingPlanQueryKeys.spendingPlans(),
    queryFn: ({ pageParam }) => spendingPlansApi.getSpendingPlans({ first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage => (lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined),
    enabled: isAuthenticated && !isAuthLoading,
    staleTime: 60 * 1000
  });
}

/**
 * Fetches a single spendingPlan by ID. Gated on authentication.
 *
 * @param id The spendingPlan ID, or `undefined` when not yet known.
 */
export function useSpendingPlan(id: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: spendingPlanQueryKeys.spendingPlan(id ?? 0),
    queryFn: () => spendingPlansApi.getSpendingPlan(id as number),
    enabled: isAuthenticated && !isAuthLoading && typeof id === 'number',
    staleTime: 60 * 1000
  });
}

/**
 * Fetches the computed cadence summary for a spendingPlan. Gated on authentication.
 *
 * @param id The spendingPlan ID, or `undefined` when not yet known.
 */
export function useSpendingPlanSummary(id: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: spendingPlanQueryKeys.summary(id ?? 0),
    queryFn: () => spendingPlansApi.getSpendingPlanSummary(id as number),
    enabled: isAuthenticated && !isAuthLoading && typeof id === 'number',
    staleTime: 30 * 1000
  });
}

/** A spendingPlan summary paired with the originating spendingPlan id. */
export interface SpendingPlanSummaryResult {
  spendingPlanId: number;
  summary: SpendingPlanSummary | undefined;
}

/**
 * Fetches the cadence summary for several spendingPlans in parallel and reports a
 * combined loading/error state. Used by the dashboard to aggregate totals across
 * all of the user's spendingPlans. Gated on authentication.
 *
 * @param spendingPlanIds The spendingPlan IDs to fetch summaries for.
 */
export function useSpendingPlanSummaries(spendingPlanIds: number[]): {
  results: SpendingPlanSummaryResult[];
  isLoading: boolean;
  isError: boolean;
} {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();
  const enabled = isAuthenticated && !isAuthLoading;

  const queries = useQueries({
    queries: spendingPlanIds.map(id => ({
      queryKey: spendingPlanQueryKeys.summary(id),
      queryFn: () => spendingPlansApi.getSpendingPlanSummary(id),
      enabled,
      staleTime: 30 * 1000
    }))
  });

  return {
    results: spendingPlanIds.map((spendingPlanId, index) => ({ spendingPlanId, summary: queries[index]?.data })),
    isLoading: enabled && queries.some(q => q.isLoading),
    isError: queries.some(q => q.isError)
  };
}

export function useCreateSpendingPlan() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateSpendingPlanRequest) => spendingPlansApi.createSpendingPlan(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.spendingPlans() });
    }
  });
}

export function useUpdateSpendingPlan(id: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: UpdateSpendingPlanRequest) => spendingPlansApi.updateSpendingPlan(id, request),
    onSuccess: updated => {
      queryClient.setQueryData(spendingPlanQueryKeys.spendingPlan(id), updated);
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.spendingPlans() });
    }
  });
}

export function useDeleteSpendingPlan() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => spendingPlansApi.deleteSpendingPlan(id),
    onSuccess: (_data, id) => {
      queryClient.removeQueries({ queryKey: spendingPlanQueryKeys.spendingPlan(id) });
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.spendingPlans() });
    }
  });
}

/**
 * Lists a spendingPlan's income line items with cursor-based infinite pagination.
 *
 * @param spendingPlanId The parent spendingPlan ID, or `undefined` when not yet known.
 */
export function useSpendingPlanIncomes(spendingPlanId: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useInfiniteQuery({
    queryKey: spendingPlanQueryKeys.incomes(spendingPlanId ?? 0),
    queryFn: ({ pageParam }) =>
      spendingPlanIncomesApi.getIncomes(spendingPlanId as number, { first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage => (lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined),
    enabled: isAuthenticated && !isAuthLoading && typeof spendingPlanId === 'number',
    staleTime: 30 * 1000
  });
}

export function useCreateIncome(spendingPlanId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateSpendingPlanIncomeRequest) =>
      spendingPlanIncomesApi.createIncome(spendingPlanId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.incomes(spendingPlanId) });
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.summary(spendingPlanId) });
    }
  });
}

export function useUpdateIncome(spendingPlanId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ incomeId, request }: { incomeId: number; request: UpdateSpendingPlanIncomeRequest }) =>
      spendingPlanIncomesApi.updateIncome(spendingPlanId, incomeId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.incomes(spendingPlanId) });
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.summary(spendingPlanId) });
    }
  });
}

export function useDeleteIncome(spendingPlanId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (incomeId: number) => spendingPlanIncomesApi.deleteIncome(spendingPlanId, incomeId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.incomes(spendingPlanId) });
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.summary(spendingPlanId) });
    }
  });
}

/**
 * Lists a spendingPlan's expense line items with cursor-based infinite pagination.
 *
 * @param spendingPlanId The parent spendingPlan ID, or `undefined` when not yet known.
 */
export function useSpendingPlanExpenses(spendingPlanId: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useInfiniteQuery({
    queryKey: spendingPlanQueryKeys.expenses(spendingPlanId ?? 0),
    queryFn: ({ pageParam }) =>
      spendingPlanExpensesApi.getExpenses(spendingPlanId as number, { first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage => (lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined),
    enabled: isAuthenticated && !isAuthLoading && typeof spendingPlanId === 'number',
    staleTime: 30 * 1000
  });
}

export function useCreateExpense(spendingPlanId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateSpendingPlanExpenseRequest) =>
      spendingPlanExpensesApi.createExpense(spendingPlanId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.expenses(spendingPlanId) });
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.summary(spendingPlanId) });
    }
  });
}

export function useUpdateExpense(spendingPlanId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ expenseId, request }: { expenseId: number; request: UpdateSpendingPlanExpenseRequest }) =>
      spendingPlanExpensesApi.updateExpense(spendingPlanId, expenseId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.expenses(spendingPlanId) });
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.summary(spendingPlanId) });
    }
  });
}

export function useDeleteExpense(spendingPlanId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (expenseId: number) => spendingPlanExpensesApi.deleteExpense(spendingPlanId, expenseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.expenses(spendingPlanId) });
      queryClient.invalidateQueries({ queryKey: spendingPlanQueryKeys.summary(spendingPlanId) });
    }
  });
}
