import apiClient from './axiosConfig';
import type { CursorPaginatedResponse } from '../types/models';
import type { CreateTagRequest, Tag, TagQueryParameters } from '../types/tags';

export const tagsApi = {
  async getTags(query?: TagQueryParameters): Promise<CursorPaginatedResponse<Tag>> {
    const params = query
      ? Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null))
      : undefined;
    const response = await apiClient.get<CursorPaginatedResponse<Tag>>('/api/v1/tags', { params });
    return response.data;
  },

  async createTag(request: CreateTagRequest): Promise<Tag> {
    const response = await apiClient.post<Tag>('/api/v1/tags', request);
    return response.data;
  }
};
