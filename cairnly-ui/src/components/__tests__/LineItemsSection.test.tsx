import { describe, it, expect, vi, beforeAll } from 'vitest';
import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { LineItemsSection } from '../spendingPlans/LineItemsSection';
import type { SpendingPlanExpense } from '../../types/spendingPlans';
import type { Tag } from '../../types/tags';
import type { Category } from '../../types/categories';

const expenses: SpendingPlanExpense[] = [
  {
    id: 1,
    userId: 1,
    spendingPlanId: 1,
    name: 'Taxable Brokerage',
    description: 'Weekly auto-invest',
    amount: 50000,
    cadence: 'Weekly',
    categoryId: 10,
    tagIds: [1],
    metadata: {},
    createdAt: '',
    updatedAt: ''
  },
  {
    id: 2,
    userId: 1,
    spendingPlanId: 1,
    name: 'Netflix',
    description: null,
    amount: 500,
    cadence: 'Monthly',
    categoryId: 11,
    tagIds: [2],
    metadata: {},
    createdAt: '',
    updatedAt: ''
  }
];

const tags: Tag[] = [
  { id: 1, userId: 1, name: 'investing', metadata: {}, createdAt: '', updatedAt: '' },
  { id: 2, userId: 1, name: 'subscription', metadata: {}, createdAt: '', updatedAt: '' }
];

const categories: Category[] = [
  {
    id: 10,
    userId: 1,
    name: 'Financial & Legal Services',
    icon: '🗄',
    parentId: 99,
    kind: 'Expense',
    isSystem: true,
    metadata: {},
    createdAt: '',
    updatedAt: ''
  },
  {
    id: 11,
    userId: 1,
    name: 'Entertainment & Recreation',
    icon: '🎥',
    parentId: 99,
    kind: 'Expense',
    isSystem: true,
    metadata: {},
    createdAt: '',
    updatedAt: ''
  }
];

const idleQuery = {
  data: undefined as unknown,
  hasNextPage: false,
  isFetchingNextPage: false,
  fetchNextPage: vi.fn(),
  isLoading: false,
  isError: false,
  error: null
};

const idleMutation = { mutateAsync: vi.fn(), isPending: false, error: null, reset: vi.fn() };

vi.mock('../../hooks/spendingPlans', () => ({
  useSpendingPlanIncomes: () => idleQuery,
  useSpendingPlanExpenses: () => ({ ...idleQuery, data: { pages: [{ nodes: expenses }] } }),
  useCreateIncome: () => idleMutation,
  useUpdateIncome: () => idleMutation,
  useDeleteIncome: () => idleMutation,
  useCreateExpense: () => idleMutation,
  useUpdateExpense: () => idleMutation,
  useDeleteExpense: () => idleMutation
}));

vi.mock('../../hooks/tags', () => ({
  useTags: () => ({ tags, tagsById: new Map(tags.map(t => [t.id, t])), isLoading: false }),
  useCreateTag: () => ({ mutateAsync: vi.fn(), isPending: false })
}));

vi.mock('../../hooks/categories', () => ({
  useCategories: () => ({
    categories,
    categoriesById: new Map(categories.map(c => [c.id, c])),
    groups: [],
    isLoading: false
  })
}));

beforeAll(() => {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
      onchange: null
    })
  });
  globalThis.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

describe('LineItemsSection (HeroUI Table)', () => {
  it('renders the expense cadence-cost columns with normalized values', () => {
    render(<LineItemsSection kind="expense" spendingPlanId={1} currency="USD" />);

    // Column headers from the HeroUI table.
    expect(screen.getByText('Frequency')).toBeInTheDocument();
    expect(screen.getByText('Daily Cost')).toBeInTheDocument();
    expect(screen.getByText('Weekly Cost')).toBeInTheDocument();
    expect(screen.getByText('Monthly Cost')).toBeInTheDocument();
    expect(screen.getByText('Annual Cost')).toBeInTheDocument();

    // Taxable Brokerage: $500/week => $71.23 daily, $2,166.67 monthly, $26,000 annual.
    expect(screen.getByText('Taxable Brokerage')).toBeInTheDocument();
    expect(screen.getByText('$71.23')).toBeInTheDocument();
    expect(screen.getByText('$2,166.67')).toBeInTheDocument();
    expect(screen.getByText('$26,000.00')).toBeInTheDocument();
  });

  it('exposes the search box and add button', () => {
    render(<LineItemsSection kind="expense" spendingPlanId={1} currency="USD" />);

    expect(screen.getByRole('searchbox', { name: /search expense/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Add expense' })).toBeInTheDocument();
  });

  it('opens the details modal when the in-cell details button is pressed', async () => {
    const user = userEvent.setup();
    render(<LineItemsSection kind="expense" spendingPlanId={1} currency="USD" />);

    await user.click(screen.getByRole('button', { name: 'View Taxable Brokerage details' }));

    // The details modal surfaces the amount-per-period sentence.
    expect(await screen.findByText(/per weekly period/i)).toBeInTheDocument();
  });

  it('searches by tag name', async () => {
    const user = userEvent.setup();
    render(<LineItemsSection kind="expense" spendingPlanId={1} currency="USD" />);

    await user.type(screen.getByRole('searchbox', { name: /search expense/i }), 'subscription');

    // Only Netflix carries the 'subscription' tag.
    expect(screen.getByText('Netflix')).toBeInTheDocument();
    expect(screen.queryByText('Taxable Brokerage')).not.toBeInTheDocument();
  });
});
