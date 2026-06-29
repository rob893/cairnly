import type {
  SpendingPlanExpense,
  SpendingPlanIncome,
  UpdateSpendingPlanExpenseRequest,
  UpdateSpendingPlanIncomeRequest
} from '../types/spendingPlans';
import type { Transaction, UpdateTransactionRequest } from '../types/transactions';

type SpendingPlanLineItem = SpendingPlanIncome | SpendingPlanExpense;
type SpendingPlanLineItemUpdateRequest = UpdateSpendingPlanIncomeRequest & UpdateSpendingPlanExpenseRequest;

/** Builds a full update request by applying a partial patch over values derived from the current item. */
export function buildPatchedRequest<TItem, TRequest extends object>(
  item: TItem,
  toRequest: (item: TItem) => TRequest,
  patch: Partial<TRequest>
): TRequest {
  return { ...toRequest(item), ...patch };
}

/** Builds a full spending-plan line item update request from the current item plus patched fields. */
export function buildSpendingPlanLineItemUpdateRequest(
  item: SpendingPlanLineItem,
  patch: Partial<SpendingPlanLineItemUpdateRequest>
): SpendingPlanLineItemUpdateRequest {
  return buildPatchedRequest(
    item,
    current => ({
      name: current.name,
      description: current.description ?? null,
      amount: current.amount,
      cadence: current.cadence,
      categoryId: current.categoryId,
      tagIds: current.tagIds
    }),
    patch
  );
}

/** Builds a full transaction update request from the current transaction plus patched fields. */
export function buildTransactionUpdateRequest(
  transaction: Transaction,
  patch: Partial<UpdateTransactionRequest>
): UpdateTransactionRequest {
  return buildPatchedRequest(
    transaction,
    current => ({
      accountId: current.accountId,
      date: current.date,
      amount: current.amount,
      merchant: current.merchant ?? null,
      description: current.description ?? null,
      categoryId: current.categoryId,
      source: current.source,
      parentTransactionId: current.parentTransactionId ?? null,
      tagIds: current.tagIds,
      metadata: current.metadata
    }),
    patch
  );
}
