import { useEffect, useState } from 'react';
import { Button, Label, Modal, TextArea, TextField } from '@heroui/react';
import { FormField } from '../FormField';
import { SelectField } from '../SelectField';
import { TagSelector } from './TagSelector';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import { minorToMajor, parseMoneyToMinor } from '../../utils/money';
import { budgetCadences, incomeTypes } from '../../types/budgets';
import type {
  BudgetCadence,
  BudgetExpense,
  BudgetIncome,
  CreateBudgetExpenseRequest,
  CreateBudgetIncomeRequest,
  IncomeType
} from '../../types/budgets';

type LineItemKind = 'income' | 'expense';

interface LineItemFormModalProps {
  kind: LineItemKind;
  currency: string;
  isOpen: boolean;
  onOpenChange(open: boolean): void;
  /** The item being edited, or `undefined` when creating a new one. */
  item?: BudgetIncome | BudgetExpense;
  /** Persists the form. The payload is a superset accepted by both income and expense create/update. */
  onSubmit(payload: CreateBudgetIncomeRequest & CreateBudgetExpenseRequest): Promise<unknown>;
  isPending: boolean;
  error: Error | null;
}

const DEFAULT_INCOME_CADENCE: BudgetCadence = 'Annual';
const DEFAULT_EXPENSE_CADENCE: BudgetCadence = 'Monthly';

/**
 * Modal form for creating or editing a budget income or expense line item.
 * Amounts are entered in major units and converted to integer minor units on save.
 */
export function LineItemFormModal({
  kind,
  currency,
  isOpen,
  onOpenChange,
  item,
  onSubmit,
  isPending,
  error
}: LineItemFormModalProps) {
  const isEdit = item !== undefined;
  const noun = kind === 'income' ? 'income' : 'expense';

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [amount, setAmount] = useState('');
  const [cadence, setCadence] = useState<BudgetCadence>(
    kind === 'income' ? DEFAULT_INCOME_CADENCE : DEFAULT_EXPENSE_CADENCE
  );
  const [type, setType] = useState<IncomeType>('W2');
  const [tagIds, setTagIds] = useState<number[]>([]);
  const [localError, setLocalError] = useState<string | null>(null);

  // Re-seed the form whenever it opens or the target item changes.
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setLocalError(null);
    setName(item?.name ?? '');
    setDescription(item?.description ?? '');
    setAmount(item ? String(minorToMajor(item.amount, currency)) : '');
    setCadence(item?.cadence ?? (kind === 'income' ? DEFAULT_INCOME_CADENCE : DEFAULT_EXPENSE_CADENCE));
    setType(item && 'type' in item ? item.type : 'W2');
    setTagIds(item?.tagIds ?? []);
  }, [isOpen, item, kind, currency]);

  const handleSubmit = async () => {
    setLocalError(null);

    if (!name.trim()) {
      setLocalError('Name is required.');
      return;
    }

    const minor = parseMoneyToMinor(amount, currency);

    if (minor === null) {
      setLocalError('Enter a valid amount.');
      return;
    }

    const payload: CreateBudgetIncomeRequest & CreateBudgetExpenseRequest = {
      name: name.trim(),
      description: description.trim() ? description.trim() : null,
      amount: minor,
      cadence,
      type,
      tagIds
    };

    await onSubmit(payload);
  };

  const title = `${isEdit ? 'Edit' : 'Add'} ${noun}`;

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange}>
      <Modal.Backdrop>
        <Modal.Container size="md">
          <Modal.Dialog>
            <Modal.CloseTrigger />
            <Modal.Header>
              <Modal.Heading>{title}</Modal.Heading>
            </Modal.Header>
            <Modal.Body className="space-y-4">
              {error && <ApiErrorDisplay error={error} title="Save failed" showDetails={showErrorDetails} />}
              {localError && (
                <div className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-sm text-danger">
                  {localError}
                </div>
              )}

              <FormField
                label="Name"
                value={name}
                onChange={setName}
                isRequired
                placeholder={kind === 'income' ? 'e.g. Salary' : 'e.g. Rent'}
              />

              <FormField
                label={`Amount (${currency.toUpperCase()})`}
                value={amount}
                onChange={setAmount}
                isRequired
                placeholder="0.00"
                description="Amount per selected cadence period."
              />

              <SelectField<BudgetCadence>
                label="Cadence"
                value={cadence}
                onChange={setCadence}
                options={budgetCadences}
              />

              {kind === 'income' && (
                <SelectField<IncomeType> label="Income type" value={type} onChange={setType} options={incomeTypes} />
              )}

              <TextField value={description} onChange={setDescription} className="w-full">
                <Label className="mb-1 block text-sm">Description</Label>
                <TextArea
                  placeholder="Optional notes"
                  className="w-full min-h-20 rounded-lg border border-border bg-surface px-3 py-2 text-sm"
                />
              </TextField>

              <TagSelector value={tagIds} onChange={setTagIds} />
            </Modal.Body>
            <Modal.Footer>
              <Button slot="close" variant="outline">
                Cancel
              </Button>
              <Button onPress={handleSubmit} isPending={isPending} isDisabled={!name.trim() || !amount.trim()}>
                {isEdit ? 'Save changes' : `Add ${noun}`}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}
