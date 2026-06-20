import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { preferencesApi, usersApi } from '../services/api';
import { useAuth } from './useAuth';
import type {
  LinkedAccountType,
  UpdatePasswordRequest,
  UpdateUsernameRequest,
  UpdateUserPreferencesRequest
} from '../types/models';

export const queryKeys = {
  userDetails: (id: number) => ['users', id] as const,
  preferences: (userId: number) => ['users', userId, 'preferences'] as const
} as const;

/**
 * Fetches the full details of a user (account page). Gated on authentication.
 *
 * @param id The user ID, or `undefined` when not yet known.
 */
export function useUserDetails(id: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: queryKeys.userDetails(id ?? 0),
    queryFn: () => usersApi.getUser(id as number),
    enabled: isAuthenticated && !isAuthLoading && typeof id === 'number',
    staleTime: 60 * 1000
  });
}

export function useUpdateUsername(id: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: UpdateUsernameRequest) => usersApi.updateUsername(id, request),
    onSuccess: updated => {
      queryClient.setQueryData(queryKeys.userDetails(id), updated);
    }
  });
}

export function useUpdatePassword(id: number) {
  return useMutation({
    mutationFn: (request: UpdatePasswordRequest) => usersApi.updatePassword(id, request)
  });
}

export function useDeleteUser(id: number) {
  return useMutation({
    mutationFn: () => usersApi.deleteUser(id)
  });
}

export function useUnlinkAccount(id: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (linkedAccountType: LinkedAccountType) => usersApi.unlinkAccount(id, linkedAccountType),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.userDetails(id) });
    }
  });
}

export function useSendEmailConfirmation(id: number) {
  return useMutation({
    mutationFn: () => usersApi.sendEmailConfirmation(id)
  });
}

/**
 * Fetches the current user's saved preferences. Gated on authentication.
 *
 * @param userId The user ID, or `undefined` when not yet known.
 */
export function usePreferences(userId: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: queryKeys.preferences(userId ?? 0),
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
      queryClient.setQueryData(queryKeys.preferences(userId), updated);
    }
  });
}
