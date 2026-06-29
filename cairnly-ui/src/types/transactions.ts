import type { CursorPaginationQueryParameters } from './pagination';

/**
 * How a transaction entered the system. Serialized as a string by the API
 * (`JsonStringEnumConverter`).
 */
export type TransactionSource = 'Manual' | 'Import';

/** A single money movement on an account, owned by the current user. */
export interface Transaction {
  id: number;
  userId: number;
  accountId: number;
  /** When the transaction occurred (ISO 8601). */
  date: string;
  /** Signed amount in integer minor units; positive for inflows. */
  amount: number;
  merchant?: string | null;
  description?: string | null;
  categoryId: number;
  source: TransactionSource;
  isSplit: boolean;
  /** Whether this transaction is a balance adjustment from reconciling the account. */
  isBalanceAdjustment: boolean;
  parentTransactionId?: number | null;
  tagIds: number[];
  metadata: Record<string, unknown>;
  createdAt: string;
  updatedAt: string;
}

/** Query parameters for listing transactions. */
export interface TransactionQueryParameters extends CursorPaginationQueryParameters {
  accountId?: number;
  categoryId?: number;
  /** Filter to transactions in any of these categories (e.g. a group's children). */
  categoryIds?: number[];
  tagId?: number;
  source?: TransactionSource;
  dateFrom?: string;
  dateTo?: string;
  minAmount?: number;
  maxAmount?: number;
  merchant?: string;
  parentTransactionId?: number;
  includeSplitChildren?: boolean;
}

// Request models

/** Payload to create a new transaction. */
export interface CreateTransactionRequest {
  accountId: number;
  /** When the transaction occurred (ISO 8601). */
  date: string;
  /** Signed amount in integer minor units; positive for inflows. */
  amount: number;
  merchant?: string | null;
  description?: string | null;
  categoryId: number;
  source?: TransactionSource;
  parentTransactionId?: number | null;
  tagIds?: number[] | null;
  metadata?: Record<string, unknown> | null;
}

/** Payload to fully update an existing transaction. */
export type UpdateTransactionRequest = CreateTransactionRequest;
