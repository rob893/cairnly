import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { preferencesApi } from '../services/preferences';
import { useAuth } from './useAuth';
import type { UpdateUserPreferencesRequest } from '../types/preferences';

export const preferenceQueryKeys = {
  preferences: (userId: number) => ['users', userId, 'preferences'] as const
} as const;

/**
 * Fetches the current user's saved preferences. Gated on authentication.
 *
 * @param userId The user ID, or `undefined` when not yet known.
 */
export function usePreferences(userId: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: preferenceQueryKeys.preferences(userId ?? 0),
    queryFn: () => preferencesApi.getPreferences(userId as number),
    enabled: isAuthenticated && !isAuthLoading && typeof userId === 'number',
    staleTime: 5 * 60 * 1000
  });
}

export function useUpdatePreferences(userId: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: UpdateUserPreferencesRequest) => preferencesApi.updatePreferences(userId, request),
    onSuccess: updated => {
      queryClient.setQueryData(preferenceQueryKeys.preferences(userId), updated);
    }
  });
}
