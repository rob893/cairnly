import apiClient from './axiosConfig';
import type { CursorPaginatedResponse } from '../types/models';
import type {
  Budget,
  BudgetExpense,
  BudgetExpenseQueryParameters,
  BudgetIncome,
  BudgetIncomeQueryParameters,
  BudgetQueryParameters,
  BudgetSummary,
  CreateBudgetExpenseRequest,
  CreateBudgetIncomeRequest,
  CreateBudgetRequest,
  UpdateBudgetExpenseRequest,
  UpdateBudgetIncomeRequest,
  UpdateBudgetRequest
} from '../types/budgets';

/**
 * Strips `undefined` values so they are not serialized into the query string,
 * returning a record axios can pass straight to `params`.
 */
function toParams<T extends object>(query: T | undefined): Record<string, unknown> | undefined {
  if (!query) {
    return undefined;
  }

  return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null));
}

export const budgetsApi = {
  async getBudgets(query?: BudgetQueryParameters): Promise<CursorPaginatedResponse<Budget>> {
    const response = await apiClient.get<CursorPaginatedResponse<Budget>>('/api/v1/budgets', {
      params: toParams(query)
    });
    return response.data;
  },

  async getBudget(id: number): Promise<Budget> {
    const response = await apiClient.get<Budget>(`/api/v1/budgets/${id}`);
    return response.data;
  },

  async getBudgetSummary(id: number): Promise<BudgetSummary> {
    const response = await apiClient.get<BudgetSummary>(`/api/v1/budgets/${id}/summary`);
    return response.data;
  },

  async createBudget(request: CreateBudgetRequest): Promise<Budget> {
    const response = await apiClient.post<Budget>('/api/v1/budgets', request);
    return response.data;
  },

  async updateBudget(id: number, request: UpdateBudgetRequest): Promise<Budget> {
    const response = await apiClient.put<Budget>(`/api/v1/budgets/${id}`, request);
    return response.data;
  },

  async deleteBudget(id: number): Promise<void> {
    await apiClient.delete(`/api/v1/budgets/${id}`);
  }
};

export const budgetIncomesApi = {
  async getIncomes(
    budgetId: number,
    query?: BudgetIncomeQueryParameters
  ): Promise<CursorPaginatedResponse<BudgetIncome>> {
    const response = await apiClient.get<CursorPaginatedResponse<BudgetIncome>>(
      `/api/v1/budgets/${budgetId}/income`,
      { params: toParams(query) }
    );
    return response.data;
  },

  async createIncome(budgetId: number, request: CreateBudgetIncomeRequest): Promise<BudgetIncome> {
    const response = await apiClient.post<BudgetIncome>(`/api/v1/budgets/${budgetId}/income`, request);
    return response.data;
  },

  async updateIncome(
    budgetId: number,
    incomeId: number,
    request: UpdateBudgetIncomeRequest
  ): Promise<BudgetIncome> {
    const response = await apiClient.put<BudgetIncome>(`/api/v1/budgets/${budgetId}/income/${incomeId}`, request);
    return response.data;
  },

  async deleteIncome(budgetId: number, incomeId: number): Promise<void> {
    await apiClient.delete(`/api/v1/budgets/${budgetId}/income/${incomeId}`);
  }
};

export const budgetExpensesApi = {
  async getExpenses(
    budgetId: number,
    query?: BudgetExpenseQueryParameters
  ): Promise<CursorPaginatedResponse<BudgetExpense>> {
    const response = await apiClient.get<CursorPaginatedResponse<BudgetExpense>>(
      `/api/v1/budgets/${budgetId}/expenses`,
      { params: toParams(query) }
    );
    return response.data;
  },

  async createExpense(budgetId: number, request: CreateBudgetExpenseRequest): Promise<BudgetExpense> {
    const response = await apiClient.post<BudgetExpense>(`/api/v1/budgets/${budgetId}/expenses`, request);
    return response.data;
  },

  async updateExpense(
    budgetId: number,
    expenseId: number,
    request: UpdateBudgetExpenseRequest
  ): Promise<BudgetExpense> {
    const response = await apiClient.put<BudgetExpense>(
      `/api/v1/budgets/${budgetId}/expenses/${expenseId}`,
      request
    );
    return response.data;
  },

  async deleteExpense(budgetId: number, expenseId: number): Promise<void> {
    await apiClient.delete(`/api/v1/budgets/${budgetId}/expenses/${expenseId}`);
  }
};
