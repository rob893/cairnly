import apiClient from './axiosConfig';
import type { CursorPaginatedResponse } from '../types/models';
import type {
  SpendingPlan,
  SpendingPlanExpense,
  SpendingPlanExpenseQueryParameters,
  SpendingPlanIncome,
  SpendingPlanIncomeQueryParameters,
  SpendingPlanQueryParameters,
  SpendingPlanSummary,
  CreateSpendingPlanExpenseRequest,
  CreateSpendingPlanIncomeRequest,
  CreateSpendingPlanRequest,
  UpdateSpendingPlanExpenseRequest,
  UpdateSpendingPlanIncomeRequest,
  UpdateSpendingPlanRequest
} from '../types/spendingPlans';

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

export const spendingPlansApi = {
  async getSpendingPlans(query?: SpendingPlanQueryParameters): Promise<CursorPaginatedResponse<SpendingPlan>> {
    const response = await apiClient.get<CursorPaginatedResponse<SpendingPlan>>('/api/v1/spending-plans', {
      params: toParams(query)
    });
    return response.data;
  },

  async getSpendingPlan(id: number): Promise<SpendingPlan> {
    const response = await apiClient.get<SpendingPlan>(`/api/v1/spending-plans/${id}`);
    return response.data;
  },

  async getSpendingPlanSummary(id: number): Promise<SpendingPlanSummary> {
    const response = await apiClient.get<SpendingPlanSummary>(`/api/v1/spending-plans/${id}/summary`);
    return response.data;
  },

  async createSpendingPlan(request: CreateSpendingPlanRequest): Promise<SpendingPlan> {
    const response = await apiClient.post<SpendingPlan>('/api/v1/spending-plans', request);
    return response.data;
  },

  async updateSpendingPlan(id: number, request: UpdateSpendingPlanRequest): Promise<SpendingPlan> {
    const response = await apiClient.put<SpendingPlan>(`/api/v1/spending-plans/${id}`, request);
    return response.data;
  },

  async deleteSpendingPlan(id: number): Promise<void> {
    await apiClient.delete(`/api/v1/spending-plans/${id}`);
  }
};

export const spendingPlanIncomesApi = {
  async getIncomes(
    spendingPlanId: number,
    query?: SpendingPlanIncomeQueryParameters
  ): Promise<CursorPaginatedResponse<SpendingPlanIncome>> {
    const response = await apiClient.get<CursorPaginatedResponse<SpendingPlanIncome>>(
      `/api/v1/spending-plans/${spendingPlanId}/income`,
      { params: toParams(query) }
    );
    return response.data;
  },

  async createIncome(spendingPlanId: number, request: CreateSpendingPlanIncomeRequest): Promise<SpendingPlanIncome> {
    const response = await apiClient.post<SpendingPlanIncome>(`/api/v1/spending-plans/${spendingPlanId}/income`, request);
    return response.data;
  },

  async updateIncome(
    spendingPlanId: number,
    incomeId: number,
    request: UpdateSpendingPlanIncomeRequest
  ): Promise<SpendingPlanIncome> {
    const response = await apiClient.put<SpendingPlanIncome>(`/api/v1/spending-plans/${spendingPlanId}/income/${incomeId}`, request);
    return response.data;
  },

  async deleteIncome(spendingPlanId: number, incomeId: number): Promise<void> {
    await apiClient.delete(`/api/v1/spending-plans/${spendingPlanId}/income/${incomeId}`);
  }
};

export const spendingPlanExpensesApi = {
  async getExpenses(
    spendingPlanId: number,
    query?: SpendingPlanExpenseQueryParameters
  ): Promise<CursorPaginatedResponse<SpendingPlanExpense>> {
    const response = await apiClient.get<CursorPaginatedResponse<SpendingPlanExpense>>(
      `/api/v1/spending-plans/${spendingPlanId}/expenses`,
      { params: toParams(query) }
    );
    return response.data;
  },

  async createExpense(spendingPlanId: number, request: CreateSpendingPlanExpenseRequest): Promise<SpendingPlanExpense> {
    const response = await apiClient.post<SpendingPlanExpense>(`/api/v1/spending-plans/${spendingPlanId}/expenses`, request);
    return response.data;
  },

  async updateExpense(
    spendingPlanId: number,
    expenseId: number,
    request: UpdateSpendingPlanExpenseRequest
  ): Promise<SpendingPlanExpense> {
    const response = await apiClient.put<SpendingPlanExpense>(
      `/api/v1/spending-plans/${spendingPlanId}/expenses/${expenseId}`,
      request
    );
    return response.data;
  },

  async deleteExpense(spendingPlanId: number, expenseId: number): Promise<void> {
    await apiClient.delete(`/api/v1/spending-plans/${spendingPlanId}/expenses/${expenseId}`);
  }
};
