import { useInfiniteQuery, useMutation, useQueries, useQuery, useQueryClient } from '@tanstack/react-query';
import { budgetExpensesApi, budgetIncomesApi, budgetsApi } from '../services/budgets';
import { useAuth } from './useAuth';
import type {
  BudgetSummary,
  CreateBudgetExpenseRequest,
  CreateBudgetIncomeRequest,
  CreateBudgetRequest,
  UpdateBudgetExpenseRequest,
  UpdateBudgetIncomeRequest,
  UpdateBudgetRequest
} from '../types/budgets';

/** Default page size for budget list endpoints. */
const PAGE_SIZE = 100;

export const budgetQueryKeys = {
  budgets: () => ['budgets'] as const,
  budget: (id: number) => ['budgets', id] as const,
  summary: (id: number) => ['budgets', id, 'summary'] as const,
  incomes: (budgetId: number) => ['budgets', budgetId, 'income'] as const,
  expenses: (budgetId: number) => ['budgets', budgetId, 'expenses'] as const
} as const;

/**
 * Lists the current user's budgets with cursor-based infinite pagination. Gated
 * on authentication.
 */
export function useBudgets() {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useInfiniteQuery({
    queryKey: budgetQueryKeys.budgets(),
    queryFn: ({ pageParam }) => budgetsApi.getBudgets({ first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage =>
      lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined,
    enabled: isAuthenticated && !isAuthLoading,
    staleTime: 60 * 1000
  });
}

/**
 * Fetches a single budget by ID. Gated on authentication.
 *
 * @param id The budget ID, or `undefined` when not yet known.
 */
export function useBudget(id: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: budgetQueryKeys.budget(id ?? 0),
    queryFn: () => budgetsApi.getBudget(id as number),
    enabled: isAuthenticated && !isAuthLoading && typeof id === 'number',
    staleTime: 60 * 1000
  });
}

/**
 * Fetches the computed cadence summary for a budget. Gated on authentication.
 *
 * @param id The budget ID, or `undefined` when not yet known.
 */
export function useBudgetSummary(id: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: budgetQueryKeys.summary(id ?? 0),
    queryFn: () => budgetsApi.getBudgetSummary(id as number),
    enabled: isAuthenticated && !isAuthLoading && typeof id === 'number',
    staleTime: 30 * 1000
  });
}

/** A budget summary paired with the originating budget id. */
export interface BudgetSummaryResult {
  budgetId: number;
  summary: BudgetSummary | undefined;
}

/**
 * Fetches the cadence summary for several budgets in parallel and reports a
 * combined loading/error state. Used by the dashboard to aggregate totals across
 * all of the user's budgets. Gated on authentication.
 *
 * @param budgetIds The budget IDs to fetch summaries for.
 */
export function useBudgetSummaries(budgetIds: number[]): {
  results: BudgetSummaryResult[];
  isLoading: boolean;
  isError: boolean;
} {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();
  const enabled = isAuthenticated && !isAuthLoading;

  const queries = useQueries({
    queries: budgetIds.map(id => ({
      queryKey: budgetQueryKeys.summary(id),
      queryFn: () => budgetsApi.getBudgetSummary(id),
      enabled,
      staleTime: 30 * 1000
    }))
  });

  return {
    results: budgetIds.map((budgetId, index) => ({ budgetId, summary: queries[index]?.data })),
    isLoading: enabled && queries.some(q => q.isLoading),
    isError: queries.some(q => q.isError)
  };
}

export function useCreateBudget() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateBudgetRequest) => budgetsApi.createBudget(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.budgets() });
    }
  });
}

export function useUpdateBudget(id: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: UpdateBudgetRequest) => budgetsApi.updateBudget(id, request),
    onSuccess: updated => {
      queryClient.setQueryData(budgetQueryKeys.budget(id), updated);
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.budgets() });
    }
  });
}

export function useDeleteBudget() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: number) => budgetsApi.deleteBudget(id),
    onSuccess: (_data, id) => {
      queryClient.removeQueries({ queryKey: budgetQueryKeys.budget(id) });
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.budgets() });
    }
  });
}

/**
 * Lists a budget's income line items with cursor-based infinite pagination.
 *
 * @param budgetId The parent budget ID, or `undefined` when not yet known.
 */
export function useBudgetIncomes(budgetId: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useInfiniteQuery({
    queryKey: budgetQueryKeys.incomes(budgetId ?? 0),
    queryFn: ({ pageParam }) =>
      budgetIncomesApi.getIncomes(budgetId as number, { first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage =>
      lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined,
    enabled: isAuthenticated && !isAuthLoading && typeof budgetId === 'number',
    staleTime: 30 * 1000
  });
}

export function useCreateIncome(budgetId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateBudgetIncomeRequest) => budgetIncomesApi.createIncome(budgetId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.incomes(budgetId) });
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.summary(budgetId) });
    }
  });
}

export function useUpdateIncome(budgetId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ incomeId, request }: { incomeId: number; request: UpdateBudgetIncomeRequest }) =>
      budgetIncomesApi.updateIncome(budgetId, incomeId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.incomes(budgetId) });
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.summary(budgetId) });
    }
  });
}

export function useDeleteIncome(budgetId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (incomeId: number) => budgetIncomesApi.deleteIncome(budgetId, incomeId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.incomes(budgetId) });
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.summary(budgetId) });
    }
  });
}

/**
 * Lists a budget's expense line items with cursor-based infinite pagination.
 *
 * @param budgetId The parent budget ID, or `undefined` when not yet known.
 */
export function useBudgetExpenses(budgetId: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useInfiniteQuery({
    queryKey: budgetQueryKeys.expenses(budgetId ?? 0),
    queryFn: ({ pageParam }) =>
      budgetExpensesApi.getExpenses(budgetId as number, { first: PAGE_SIZE, after: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: lastPage =>
      lastPage.pageInfo.hasNextPage ? lastPage.pageInfo.endCursor : undefined,
    enabled: isAuthenticated && !isAuthLoading && typeof budgetId === 'number',
    staleTime: 30 * 1000
  });
}

export function useCreateExpense(budgetId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: CreateBudgetExpenseRequest) => budgetExpensesApi.createExpense(budgetId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.expenses(budgetId) });
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.summary(budgetId) });
    }
  });
}

export function useUpdateExpense(budgetId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ expenseId, request }: { expenseId: number; request: UpdateBudgetExpenseRequest }) =>
      budgetExpensesApi.updateExpense(budgetId, expenseId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.expenses(budgetId) });
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.summary(budgetId) });
    }
  });
}

export function useDeleteExpense(budgetId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (expenseId: number) => budgetExpensesApi.deleteExpense(budgetId, expenseId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.expenses(budgetId) });
      queryClient.invalidateQueries({ queryKey: budgetQueryKeys.summary(budgetId) });
    }
  });
}
