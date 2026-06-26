import apiClient from './axiosConfig';
import type { LinkedAccountType, UpdatePasswordRequest, UpdateUsernameRequest, UserDetails } from '../types/users';

export const usersApi = {
  async getUser(id: number): Promise<UserDetails> {
    const response = await apiClient.get<UserDetails>(`/api/v1/users/${id}`);
    return response.data;
  },

  async updateUsername(id: number, request: UpdateUsernameRequest): Promise<UserDetails> {
    const response = await apiClient.put<UserDetails>(`/api/v1/users/${id}/username`, request);
    return response.data;
  },

  async updatePassword(id: number, request: UpdatePasswordRequest): Promise<void> {
    await apiClient.put(`/api/v1/users/${id}/password`, request);
  },

  async deleteUser(id: number): Promise<void> {
    await apiClient.delete(`/api/v1/users/${id}`);
  },

  async unlinkAccount(id: number, linkedAccountType: LinkedAccountType): Promise<void> {
    await apiClient.delete(`/api/v1/users/${id}/linkedAccounts/${linkedAccountType}`);
  },

  async sendEmailConfirmation(id: number): Promise<void> {
    await apiClient.post(`/api/v1/users/${id}/emailConfirmations`);
  }
};
