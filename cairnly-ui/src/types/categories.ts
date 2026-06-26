import type { CursorPaginationQueryParameters } from './pagination';

/** The kind of money movement a category groups. Serialized as a string by the API. */
export type CategoryKind = 'Income' | 'Expense' | 'Transfer';

/** A spending category, either system-defined (shared) or user-defined. */
export interface Category {
  id: number;
  userId: number;
  name: string;
  /** Optional emoji representing the category. */
  icon?: string | null;
  /** Parent (group) category ID, or null/undefined for a top-level group. */
  parentId?: number | null;
  kind: CategoryKind;
  isSystem: boolean;
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

/** Payload to create a new category. */
export interface CreateCategoryRequest {
  name: string;
  icon?: string | null;
  parentId?: number | null;
  kind: CategoryKind;
  metadata?: Record<string, unknown> | null;
}

/** Query parameters for listing categories. */
export interface CategoryQueryParameters extends CursorPaginationQueryParameters {
  name?: string;
  kind?: CategoryKind;
}
