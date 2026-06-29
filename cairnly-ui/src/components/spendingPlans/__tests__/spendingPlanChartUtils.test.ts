import { describe, expect, it } from 'vitest';
import {
  buildComposition,
  buildFlow,
  compactMoney,
  getFlowLinkTarget,
  isFlowNodePayload,
  type FlowNode,
  type FlowNodePayload
} from '../spendingPlanChartUtils';
import type { Category } from '../../../types/categories';
import type { SpendingPlanExpense, SpendingPlanIncome } from '../../../types/spendingPlans';

const categories = new Map<number, Category>([
  [1, category(1, 'Paychecks', 'Income', '💼')],
  [2, category(2, 'Bills', 'Expense', '🏠')],
  [3, category(3, 'Rent', 'Expense', '🏡', 2)],
  [4, category(4, 'Food', 'Expense', '🍔')]
]);

function category(id: number, name: string, kind: Category['kind'], icon?: string, parentId?: number): Category {
  return {
    id,
    userId: 1,
    name,
    icon,
    parentId,
    kind,
    isSystem: false,
    metadata: {},
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z'
  };
}

function income(overrides: Partial<SpendingPlanIncome>): SpendingPlanIncome {
  return {
    id: 1,
    userId: 1,
    spendingPlanId: 1,
    name: 'Salary',
    description: null,
    amount: 500000,
    cadence: 'Monthly',
    categoryId: 1,
    tagIds: [],
    metadata: {},
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides
  };
}

function expense(overrides: Partial<SpendingPlanExpense>): SpendingPlanExpense {
  return {
    id: 1,
    userId: 1,
    spendingPlanId: 1,
    name: 'Rent',
    description: null,
    amount: 200000,
    cadence: 'Monthly',
    categoryId: 3,
    tagIds: [],
    metadata: {},
    createdAt: '2026-01-01T00:00:00Z',
    updatedAt: '2026-01-01T00:00:00Z',
    ...overrides
  };
}

describe('spendingPlanChartUtils', () => {
  it('aggregates donut composition by category and rolls extra slices into Other', () => {
    const slices = buildComposition(
      [
        income({ id: 1, name: 'Salary', amount: 600000, categoryId: 1 }),
        income({ id: 2, name: 'Bonus', amount: 100000, categoryId: 1 }),
        income({ id: 3, name: 'Side A', amount: 90000, categoryId: 10 }),
        income({ id: 4, name: 'Side B', amount: 80000, categoryId: 11 }),
        income({ id: 5, name: 'Side C', amount: 70000, categoryId: 12 }),
        income({ id: 6, name: 'Side D', amount: 60000, categoryId: 13 }),
        income({ id: 7, name: 'Side E', amount: 50000, categoryId: 14 }),
        income({ id: 8, name: 'Side F', amount: 40000, categoryId: 15 })
      ],
      categories,
      'category'
    );

    expect(slices[0]).toMatchObject({ name: '💼 Paychecks', value: 700000, color: 'var(--accent)' });
    expect(slices[slices.length - 1]).toMatchObject({ name: 'Other (1)', value: 40000, color: '#94a3b8' });
  });

  it('builds category-group Sankey flow with themed income, savings, and expense colors', () => {
    const flow = buildFlow(
      [income({ amount: 500000 })],
      [
        expense({ id: 1, amount: 200000, categoryId: 3 }),
        expense({ id: 2, name: 'Groceries', amount: 100000, categoryId: 4 })
      ],
      categories,
      'category-group'
    );

    expect(flow?.totalIncome).toBe(500000);
    expect(flow?.nodes).toEqual(
      expect.arrayContaining<FlowNode>([
        { name: 'Income', color: 'var(--success)' },
        { name: 'Paychecks', color: 'var(--success)', icon: '💼' },
        { name: 'Savings', color: 'var(--success)', icon: '💰' },
        { name: 'Bills', color: '#fb923c', icon: '🏠' },
        { name: 'Rent', color: '#fb923c', icon: '🏡' },
        { name: 'Food', color: '#f472b6', icon: '🍔' }
      ])
    );
    expect(flow?.links).toEqual(expect.arrayContaining([{ source: 0, target: 2, value: 200000 }]));
  });

  it('narrows Recharts Sankey node and link payloads without unsafe casts', () => {
    const target: FlowNodePayload = { name: 'Rent', color: '#fb923c', value: 200000, depth: 2 };
    const payload = { source: { name: 'Income', color: 'var(--success)' }, target, value: 200000 };

    expect(isFlowNodePayload(target)).toBe(true);
    expect(isFlowNodePayload({ name: 'Rent', color: '#fb923c' })).toBe(false);
    expect(getFlowLinkTarget(payload)).toBe(target);
    expect(getFlowLinkTarget({ target: { name: 'Broken' } })).toBeUndefined();
  });

  it('formats compact money labels from minor units', () => {
    expect(compactMoney(123456, 'USD')).toBe('$1k');
    expect(compactMoney(-999, 'USD')).toBe('-$10');
  });
});
