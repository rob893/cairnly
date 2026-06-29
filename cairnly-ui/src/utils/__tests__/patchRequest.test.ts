import { describe, expect, it } from 'vitest';
import {
  buildPatchedRequest,
  buildSpendingPlanLineItemUpdateRequest,
  buildTransactionUpdateRequest
} from '../patchRequest';
import type { SpendingPlanExpense } from '../../types/spendingPlans';
import type { Transaction } from '../../types/transactions';

describe('buildPatchedRequest', () => {
  it('merges a patch over a full request built from the current item', () => {
    const result = buildPatchedRequest(
      { id: 1, name: 'Original', categoryId: 2 },
      item => ({ name: item.name, categoryId: item.categoryId }),
      { categoryId: 3 }
    );

    expect(result).toEqual({ name: 'Original', categoryId: 3 });
  });
});

describe('buildSpendingPlanLineItemUpdateRequest', () => {
  it('preserves existing line item fields while applying inline edit patches', () => {
    const item: SpendingPlanExpense = {
      id: 10,
      userId: 20,
      spendingPlanId: 30,
      name: 'Rent',
      description: undefined,
      amount: 125000,
      cadence: 'Monthly',
      categoryId: 40,
      tagIds: [1, 2],
      metadata: { source: 'seed' },
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-02T00:00:00Z'
    };

    const result = buildSpendingPlanLineItemUpdateRequest(item, { name: 'Mortgage' });

    expect(result).toEqual({
      name: 'Mortgage',
      description: null,
      amount: 125000,
      cadence: 'Monthly',
      categoryId: 40,
      tagIds: [1, 2]
    });
  });
});

describe('buildTransactionUpdateRequest', () => {
  it('preserves existing transaction fields while applying inline edit patches', () => {
    const transaction: Transaction = {
      id: 10,
      userId: 20,
      accountId: 30,
      date: '2026-01-01T00:00:00Z',
      amount: -1234,
      merchant: undefined,
      description: undefined,
      categoryId: 40,
      source: 'Manual',
      isSplit: false,
      isBalanceAdjustment: false,
      parentTransactionId: undefined,
      tagIds: [1, 2],
      metadata: { source: 'seed' },
      createdAt: '2026-01-01T00:00:00Z',
      updatedAt: '2026-01-02T00:00:00Z'
    };

    const result = buildTransactionUpdateRequest(transaction, { merchant: 'Cafe' });

    expect(result).toEqual({
      accountId: 30,
      date: '2026-01-01T00:00:00Z',
      amount: -1234,
      merchant: 'Cafe',
      description: null,
      categoryId: 40,
      source: 'Manual',
      parentTransactionId: null,
      tagIds: [1, 2],
      metadata: { source: 'seed' }
    });
  });
});
