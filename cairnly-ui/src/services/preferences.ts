import apiClient from './axiosConfig';
import type { UpdateUserPreferencesRequest, UserPreferences } from '../types/preferences';

export const preferencesApi = {
  async getPreferences(userId: number): Promise<UserPreferences> {
    const response = await apiClient.get<UserPreferences>(`/api/v1/users/${userId}/preferences`);
    return response.data;
  },

  async updatePreferences(userId: number, request: UpdateUserPreferencesRequest): Promise<UserPreferences> {
    const response = await apiClient.put<UserPreferences>(`/api/v1/users/${userId}/preferences`, request);
    return response.data;
  }
};
