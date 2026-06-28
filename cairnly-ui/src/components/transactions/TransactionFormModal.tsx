import { useEffect, useMemo, useState } from 'react';
import { Button, Label, Modal, TextArea, TextField } from '@heroui/react';
import { FormField } from '../FormField';
import { SelectField } from '../SelectField';
import { CategorySelect } from '../CategorySelect';
import { DateField } from '../DateField';
import { TagSelector } from '../spendingPlans/TagSelector';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import { minorToMajor, parseMoneyToMinor } from '../../utils/money';
import { useCategories } from '../../hooks/categories';
import type { Account } from '../../types/accounts';
import type { CreateTransactionRequest, Transaction, TransactionSource } from '../../types/transactions';

interface TransactionFormModalProps {
  accounts: Account[];
  /** When set, the account field is locked to this account. */
  lockedAccountId?: number;
  isOpen: boolean;
  onOpenChange(open: boolean): void;
  /** The transaction being edited, or `undefined` when creating a new one. */
  item?: Transaction;
  onSubmit(payload: CreateTransactionRequest): Promise<unknown>;
  isPending: boolean;
  error: Error | null;
}

const SOURCE_OPTIONS: ReadonlyArray<{ value: TransactionSource; label: string }> = [
  { value: 'Manual', label: 'Manual' },
  { value: 'Import', label: 'Import' }
];

/** Today's date as a `YYYY-MM-DD` string for the date input default. */
function todayInput(): string {
  return new Date().toISOString().slice(0, 10);
}

/**
 * Modal form for creating or editing a transaction. The amount is entered in major
 * units (signed: positive for inflows, negative for outflows) and converted to
 * integer minor units on save.
 */
export function TransactionFormModal({
  accounts,
  lockedAccountId,
  isOpen,
  onOpenChange,
  item,
  onSubmit,
  isPending,
  error
}: TransactionFormModalProps) {
  const isEdit = item !== undefined;

  const [accountId, setAccountId] = useState<number | undefined>(lockedAccountId ?? accounts[0]?.id);
  const [date, setDate] = useState(todayInput());
  const [merchant, setMerchant] = useState('');
  const [description, setDescription] = useState('');
  const [categoryId, setCategoryId] = useState<number | null>(null);
  const [amount, setAmount] = useState('');
  const [source, setSource] = useState<TransactionSource>('Manual');
  const [tagIds, setTagIds] = useState<number[]>([]);
  const [localError, setLocalError] = useState<string | null>(null);

  const { categories } = useCategories();
  const uncategorizedId = useMemo(
    () => categories.find(category => category.name === 'Uncategorized')?.id ?? null,
    [categories]
  );

  const currency = useMemo(
    () => accounts.find(account => account.id === accountId)?.currency ?? accounts[0]?.currency ?? 'USD',
    [accounts, accountId]
  );

  const accountOptions = useMemo(
    () => accounts.map(account => ({ value: String(account.id), label: account.name })),
    [accounts]
  );

  // Re-seed the form whenever it opens or the target transaction changes.
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const seedCurrency =
      accounts.find(account => account.id === (item?.accountId ?? lockedAccountId))?.currency ?? 'USD';

    setLocalError(null);
    setAccountId(item?.accountId ?? lockedAccountId ?? accounts[0]?.id);
    setDate(item ? item.date.slice(0, 10) : todayInput());
    setMerchant(item?.merchant ?? '');
    setDescription(item?.description ?? '');
    setCategoryId(item?.categoryId ?? uncategorizedId);
    setAmount(item ? String(minorToMajor(item.amount, seedCurrency)) : '');
    setSource(item?.source ?? 'Manual');
    setTagIds(item?.tagIds ?? []);
  }, [isOpen, item, lockedAccountId, accounts, uncategorizedId]);

  const handleSubmit = async () => {
    setLocalError(null);

    if (accountId === undefined) {
      setLocalError('Select an account.');
      return;
    }

    if (categoryId === null) {
      setLocalError('Select a category.');
      return;
    }

    if (!merchant.trim()) {
      setLocalError('Merchant is required.');
      return;
    }

    const minor = parseMoneyToMinor(amount, currency);

    if (minor === null) {
      setLocalError('Enter a valid amount.');
      return;
    }

    if (!date) {
      setLocalError('Select a date.');
      return;
    }

    const payload: CreateTransactionRequest = {
      accountId,
      date: new Date(`${date}T00:00:00.000Z`).toISOString(),
      amount: minor,
      merchant: merchant.trim(),
      description: description.trim() ? description.trim() : null,
      categoryId,
      source,
      tagIds
    };

    await onSubmit(payload);
  };

  const title = `${isEdit ? 'Edit' : 'Add'} transaction`;

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

              {lockedAccountId === undefined && (
                <SelectField
                  label="Account"
                  value={accountId !== undefined ? String(accountId) : ''}
                  onChange={value => setAccountId(Number(value))}
                  options={accountOptions}
                  isRequired
                />
              )}

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <DateField label="Date" value={date} onChange={setDate} isRequired />
                <FormField
                  label={`Amount (${currency.toUpperCase()})`}
                  value={amount}
                  onChange={setAmount}
                  isRequired
                  placeholder="0.00"
                  description="Negative for outflows."
                />
              </div>

              <FormField label="Merchant" value={merchant} onChange={setMerchant} isRequired placeholder="e.g. Microsoft" />

              <CategorySelect label="Category" value={categoryId} onChange={setCategoryId} isRequired />

              <SelectField<TransactionSource>
                label="Source"
                value={source}
                onChange={setSource}
                options={SOURCE_OPTIONS}
              />

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
              <Button onPress={handleSubmit} isPending={isPending} isDisabled={!amount.trim() || !merchant.trim() || categoryId === null}>
                {isEdit ? 'Save changes' : 'Add transaction'}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}
