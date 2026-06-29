import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '../services/users';
import { useAuth } from './useAuth';
import type { LinkedAccountType, UpdatePasswordRequest, UpdateUsernameRequest } from '../types/users';

export const userQueryKeys = {
  userDetails: (id: number) => ['users', id] as const
} as const;

/**
 * Fetches the full details of a user (settings page). Gated on authentication.
 *
 * @param id The user ID, or `undefined` when not yet known.
 */
export function useUserDetails(id: number | undefined) {
  const { isLoading: isAuthLoading, isAuthenticated } = useAuth();

  return useQuery({
    queryKey: userQueryKeys.userDetails(id ?? 0),
    queryFn: () => usersApi.getUser(id as number),
    enabled: isAuthenticated && !isAuthLoading && typeof id === 'number'
  });
}

export function useUpdateUsername(id: number) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (request: UpdateUsernameRequest) => usersApi.updateUsername(id, request),
    onSuccess: updated => {
      queryClient.setQueryData(userQueryKeys.userDetails(id), updated);
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
      queryClient.invalidateQueries({ queryKey: userQueryKeys.userDetails(id) });
    }
  });
}

export function useSendEmailConfirmation(id: number) {
  return useMutation({
    mutationFn: () => usersApi.sendEmailConfirmation(id)
  });
}
