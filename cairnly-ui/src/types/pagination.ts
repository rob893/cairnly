// Generic cursor-pagination types shared across all paginated API responses.

export interface PageInfo {
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  startCursor?: string;
  endCursor?: string;
  totalCount?: number;
}

export interface Edge<T> {
  node: T;
  cursor: string;
}

export interface CursorPaginatedResponse<T> {
  nodes?: T[];
  edges?: Edge<T>[];
  pageInfo: PageInfo;
}

export interface CursorPaginationQueryParameters {
  first?: number;
  after?: string;
  last?: number;
  before?: string;
}
