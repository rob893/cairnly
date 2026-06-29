import { useQuery } from '@tanstack/react-query';
import { reportsApi } from '../services/reports';
import { useAuth } from './useAuth';
import type { BalanceHistoryTimeframe } from '../types/accounts';
import type { CashFlowPeriod } from '../types/reports';

export const reportQueryKeys = {
  cashFlow: (timeframe: BalanceHistoryTimeframe, period: CashFlowPeriod) =>
    ['reports', 'cashflow', timeframe, period] as const
} as const;

/**
 * Fetches the current user's cash-flow report for the selected window and bucket
 * granularity. Gated on authentication.
 *
 * @param timeframe The trailing window to report on.
 * @param period The bucket granularity (monthly/quarterly/yearly).
 */
export function useCashFlowReport(timeframe: BalanceHistoryTimeframe, period: CashFlowPeriod) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: reportQueryKeys.cashFlow(timeframe, period),
    queryFn: () => reportsApi.getCashFlow(timeframe, period),
    enabled: isAuthenticated && !isAuthLoading
  });
}
