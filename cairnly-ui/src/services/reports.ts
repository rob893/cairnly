import apiClient from './axiosConfig';
import type { BalanceHistoryTimeframe } from '../types/accounts';
import type { CashFlowPeriod, CashFlowReport } from '../types/reports';

export const reportsApi = {
  async getCashFlow(timeframe: BalanceHistoryTimeframe, period: CashFlowPeriod): Promise<CashFlowReport> {
    const response = await apiClient.get<CashFlowReport>('/api/v1/reports/cashflow', {
      params: { timeframe, period }
    });
    return response.data;
  }
};
