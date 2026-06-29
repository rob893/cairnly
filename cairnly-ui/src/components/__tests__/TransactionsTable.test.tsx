import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeAll, beforeEach, describe, expect, it, vi } from 'vitest';
import { TransactionsTable } from '../transactions/TransactionsTable';
import type { Account } from '../../types/accounts';
import type { Category } from '../../types/categories';
import type { Transaction } from '../../types/transactions';

const fetchNextPage = vi.fn();

const account: Account = {
  id: 1,
  userId: 1,
  name: 'Checking',
  type: 'Checking',
  class: 'Asset',
  currency: 'USD',
  openingBalance: 0,
  currentBalance: 0,
  metadata: {},
  createdAt: '',
  updatedAt: ''
};

const category: Category = {
  id: 10,
  userId: 1,
  name: 'Groceries',
  icon: '🛒',
  parentId: null,
  kind: 'Expense',
  isSystem: false,
  metadata: {},
  createdAt: '',
  updatedAt: ''
};

const transaction: Transaction = {
  id: 100,
  userId: 1,
  accountId: account.id,
  date: '2026-06-28T12:00:00Z',
  amount: -1234,
  merchant: 'Market',
  description: null,
  categoryId: category.id,
  source: 'Manual',
  isSplit: false,
  isBalanceAdjustment: false,
  parentTransactionId: null,
  tagIds: [],
  metadata: {},
  createdAt: '',
  updatedAt: ''
};

vi.mock('../../hooks/transactions', () => ({
  useTransactions: () => ({
    data: {
      pages: [{ nodes: [transaction], pageInfo: { hasNextPage: true, hasPreviousPage: false, endCursor: 'next' } }]
    },
    hasNextPage: true,
    isFetchingNextPage: false,
    fetchNextPage,
    isLoading: false,
    isError: false,
    error: null
  }),
  useCreateTransaction: () => ({ mutateAsync: vi.fn(), isPending: false, error: null, reset: vi.fn() }),
  useUpdateTransaction: () => ({ mutateAsync: vi.fn(), isPending: false, error: null, reset: vi.fn() }),
  useDeleteTransaction: () => ({ mutateAsync: vi.fn(), isPending: false, error: null, reset: vi.fn() })
}));

vi.mock('../../hooks/accounts', () => ({
  useAccounts: () => ({
    data: { pages: [{ nodes: [account], pageInfo: { hasNextPage: false, hasPreviousPage: false } }] },
    hasNextPage: false,
    isFetchingNextPage: false,
    fetchNextPage: vi.fn()
  })
}));

vi.mock('../../hooks/categories', () => ({
  useCategories: () => ({ categories: [category], categoriesById: new Map([[category.id, category]]) })
}));

beforeAll(() => {
  const rect = vi.fn(() => ({
    x: 0,
    y: 0,
    top: 0,
    left: 0,
    bottom: 600,
    right: 800,
    width: 800,
    height: 600,
    toJSON: () => {}
  }));
  Element.prototype.getBoundingClientRect = rect;
  HTMLElement.prototype.getBoundingClientRect = rect;
  globalThis.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

beforeEach(() => {
  fetchNextPage.mockClear();
});

describe('TransactionsTable paging', () => {
  it('does not drain additional pages until the user asks for more', async () => {
    render(<TransactionsTable />);

    expect(await screen.findByText('Market')).toBeInTheDocument();
    await waitFor(() => expect(fetchNextPage).not.toHaveBeenCalled());

    await userEvent.click(screen.getByRole('button', { name: 'Load more transactions' }));

    expect(fetchNextPage).toHaveBeenCalledOnce();
  });
});
