import { useEffect, useState } from 'react';
import { Button, Modal, Switch } from '@heroui/react';
import { FormField } from '../FormField';
import { SelectField } from '../SelectField';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import { minorToMajor, parseMoneyToMinor } from '../../utils/money';
import { accountClassLabel, accountTypes, defaultClassForType } from '../../types/accounts';
import type { Account, AccountClass, AccountType, CreateAccountRequest } from '../../types/accounts';

interface AccountFormModalProps {
  /** Default currency for new accounts (the page's prevailing currency). */
  defaultCurrency: string;
  isOpen: boolean;
  onOpenChange(open: boolean): void;
  /** The account being edited, or `undefined` when creating a new one. */
  item?: Account;
  onSubmit(payload: CreateAccountRequest): Promise<unknown>;
  isPending: boolean;
  error: Error | null;
}

const TYPE_OPTIONS = accountTypes.map(type => ({ value: type.value, label: type.label }));

const CLASS_OPTIONS: ReadonlyArray<{ value: AccountClass; label: string }> = [
  { value: 'Asset', label: accountClassLabel.Asset },
  { value: 'Liability', label: accountClassLabel.Liability }
];

const DEFAULT_TYPE: AccountType = 'Checking';

/**
 * Modal form for creating or editing a financial account. Balances are entered in
 * major units and converted to integer minor units on save. When "manual balance"
 * is on, the current balance is entered directly; otherwise it is derived from the
 * account's transactions.
 */
export function AccountFormModal({
  defaultCurrency,
  isOpen,
  onOpenChange,
  item,
  onSubmit,
  isPending,
  error
}: AccountFormModalProps) {
  const isEdit = item !== undefined;

  const [name, setName] = useState('');
  const [type, setType] = useState<AccountType>(DEFAULT_TYPE);
  const [accountClass, setAccountClass] = useState<AccountClass>(defaultClassForType(DEFAULT_TYPE));
  const [currency, setCurrency] = useState(defaultCurrency);
  const [openingBalance, setOpeningBalance] = useState('');
  const [currentBalance, setCurrentBalance] = useState('');
  const [isManual, setIsManual] = useState(false);
  const [localError, setLocalError] = useState<string | null>(null);

  // Re-seed the form whenever it opens or the target account changes.
  useEffect(() => {
    if (!isOpen) {
      return;
    }

    const seedCurrency = item?.currency ?? defaultCurrency;

    setLocalError(null);
    setName(item?.name ?? '');
    setType(item?.type ?? DEFAULT_TYPE);
    setAccountClass(item?.class ?? defaultClassForType(item?.type ?? DEFAULT_TYPE));
    setCurrency(seedCurrency);
    setOpeningBalance(item ? String(minorToMajor(item.openingBalance, seedCurrency)) : '');
    setCurrentBalance(item ? String(minorToMajor(item.currentBalance, seedCurrency)) : '');
    setIsManual(item?.isManual ?? false);
  }, [isOpen, item, defaultCurrency]);

  // Default the class from the type so liability types don't sit under Asset.
  const handleTypeChange = (next: AccountType) => {
    setType(next);
    setAccountClass(defaultClassForType(next));
  };

  const handleSubmit = async () => {
    setLocalError(null);

    if (!name.trim()) {
      setLocalError('Name is required.');
      return;
    }

    const normalizedCurrency = currency.trim().toUpperCase();

    if (!/^[A-Z]{3}$/.test(normalizedCurrency)) {
      setLocalError('Enter a 3-letter ISO currency code (e.g. USD).');
      return;
    }

    const openingMinor = parseMoneyToMinor(openingBalance || '0', normalizedCurrency);

    if (openingMinor === null) {
      setLocalError('Enter a valid opening balance.');
      return;
    }

    let currentMinor = 0;

    if (isManual) {
      const parsed = parseMoneyToMinor(currentBalance || '0', normalizedCurrency);

      if (parsed === null) {
        setLocalError('Enter a valid current balance.');
        return;
      }

      currentMinor = parsed;
    }

    const payload: CreateAccountRequest = {
      name: name.trim(),
      type,
      class: accountClass,
      currency: normalizedCurrency,
      openingBalance: openingMinor,
      currentBalance: currentMinor,
      isManual
    };

    await onSubmit(payload);
  };

  const title = `${isEdit ? 'Edit' : 'Add'} account`;

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
                placeholder="e.g. Checking ····1234"
              />

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <SelectField<AccountType> label="Type" value={type} onChange={handleTypeChange} options={TYPE_OPTIONS} />
                <SelectField<AccountClass>
                  label="Class"
                  value={accountClass}
                  onChange={setAccountClass}
                  options={CLASS_OPTIONS}
                />
              </div>

              <FormField
                label="Currency"
                value={currency}
                onChange={setCurrency}
                isRequired
                placeholder="USD"
                description="3-letter ISO 4217 code."
              />

              <FormField
                label={`Opening balance (${currency.trim().toUpperCase() || 'USD'})`}
                value={openingBalance}
                onChange={setOpeningBalance}
                placeholder="0.00"
                description="Starting balance; derived accounts add transactions to this."
              />

              <Switch isSelected={isManual} onChange={setIsManual} className="flex items-center gap-3">
                <Switch.Control>
                  <Switch.Thumb />
                </Switch.Control>
                <Switch.Content>
                  <span className="text-sm">Manually track this balance</span>
                </Switch.Content>
              </Switch>

              {isManual && (
                <FormField
                  label={`Current balance (${currency.trim().toUpperCase() || 'USD'})`}
                  value={currentBalance}
                  onChange={setCurrentBalance}
                  placeholder="0.00"
                  description="The balance you maintain by hand."
                />
              )}
            </Modal.Body>
            <Modal.Footer>
              <Button slot="close" variant="outline">
                Cancel
              </Button>
              <Button onPress={handleSubmit} isPending={isPending} isDisabled={!name.trim()}>
                {isEdit ? 'Save changes' : 'Add account'}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}
