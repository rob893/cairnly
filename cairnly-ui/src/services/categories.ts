import apiClient from './axiosConfig';
import type { CursorPaginatedResponse } from '../types/pagination';
import type { Category, CategoryQueryParameters, CreateCategoryRequest } from '../types/categories';

export const categoriesApi = {
  async getCategories(query?: CategoryQueryParameters): Promise<CursorPaginatedResponse<Category>> {
    const params = query
      ? Object.fromEntries(Object.entries(query).filter(([, value]) => value !== undefined && value !== null))
      : undefined;
    const response = await apiClient.get<CursorPaginatedResponse<Category>>('/api/v1/categories', { params });
    return response.data;
  },

  async createCategory(request: CreateCategoryRequest): Promise<Category> {
    const response = await apiClient.post<Category>('/api/v1/categories', request);
    return response.data;
  }
};
