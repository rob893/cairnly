import apiClient from './axiosConfig';
import type { CursorPaginatedResponse } from '../types/pagination';
import type {
  Account,
  AccountHistory,
  AccountQueryParameters,
  BalanceHistoryTimeframe,
  CreateAccountRequest,
  NetWorthHistory,
  SetAccountBalanceRequest,
  UpdateAccountRequest
} from '../types/accounts';

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

export const accountsApi = {
  async getAccounts(query?: AccountQueryParameters): Promise<CursorPaginatedResponse<Account>> {
    const response = await apiClient.get<CursorPaginatedResponse<Account>>('/api/v1/accounts', {
      params: toParams(query)
    });
    return response.data;
  },

  async getAccount(id: number): Promise<Account> {
    const response = await apiClient.get<Account>(`/api/v1/accounts/${id}`);
    return response.data;
  },

  async createAccount(request: CreateAccountRequest): Promise<Account> {
    const response = await apiClient.post<Account>('/api/v1/accounts', request);
    return response.data;
  },

  async updateAccount(id: number, request: UpdateAccountRequest): Promise<Account> {
    const response = await apiClient.put<Account>(`/api/v1/accounts/${id}`, request);
    return response.data;
  },

  async deleteAccount(id: number): Promise<void> {
    await apiClient.delete(`/api/v1/accounts/${id}`);
  },

  async setBalance(id: number, request: SetAccountBalanceRequest): Promise<Account> {
    const response = await apiClient.put<Account>(`/api/v1/accounts/${id}/balance`, request);
    return response.data;
  },

  async getNetWorthHistory(timeframe: BalanceHistoryTimeframe): Promise<NetWorthHistory> {
    const response = await apiClient.get<NetWorthHistory>('/api/v1/accounts/net-worth', {
      params: { timeframe }
    });
    return response.data;
  },

  async getAccountHistory(timeframe: BalanceHistoryTimeframe): Promise<AccountHistory[]> {
    const response = await apiClient.get<AccountHistory[]>('/api/v1/accounts/history', {
      params: { timeframe }
    });
    return response.data;
  }
};
