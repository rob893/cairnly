import apiClient from './axiosConfig';
import type { CursorPaginatedResponse } from '../types/pagination';
import type {
  CreateTransactionRequest,
  Transaction,
  TransactionQueryParameters,
  UpdateTransactionRequest
} from '../types/transactions';

/**
 * Strips `undefined`/`null` values so they are not serialized into the query
 * string, returning a record axios can pass straight to `params`.
 */
function toParams<T extends object>(query: T | undefined): Record<string, unknown> | undefined {
  if (!query) {
    return undefined;
  }

  return Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null));
}

export const transactionsApi = {
  async getTransactions(query?: TransactionQueryParameters): Promise<CursorPaginatedResponse<Transaction>> {
    const response = await apiClient.get<CursorPaginatedResponse<Transaction>>('/api/v1/transactions', {
      params: toParams(query)
    });
    return response.data;
  },

  async getTransaction(id: number): Promise<Transaction> {
    const response = await apiClient.get<Transaction>(`/api/v1/transactions/${id}`);
    return response.data;
  },

  async createTransaction(request: CreateTransactionRequest): Promise<Transaction> {
    const response = await apiClient.post<Transaction>('/api/v1/transactions', request);
    return response.data;
  },

  async updateTransaction(id: number, request: UpdateTransactionRequest): Promise<Transaction> {
    const response = await apiClient.put<Transaction>(`/api/v1/transactions/${id}`, request);
    return response.data;
  },

  async deleteTransaction(id: number): Promise<void> {
    await apiClient.delete(`/api/v1/transactions/${id}`);
  }
};
