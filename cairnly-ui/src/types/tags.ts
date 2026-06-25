import type { CursorPaginationQueryParameters } from './models';

/** A user-scoped tag that can be linked to budget income/expense line items. */
export interface Tag {
  id: number;
  userId: number;
  name: string;
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

/** Payload to create a new tag. */
export interface CreateTagRequest {
  name: string;
  metadata?: Record<string, unknown> | null;
}

/** Query parameters for listing tags. */
export interface TagQueryParameters extends CursorPaginationQueryParameters {
  name?: string;
}
