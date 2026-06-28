import { useEffect, useState } from 'react';
import { Button, Modal } from '@heroui/react';
import { FormField } from '../FormField';
import { DateField } from '../DateField';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import { minorToMajor, parseMoneyToMinor } from '../../utils/money';
import type { SetAccountBalanceRequest } from '../../types/accounts';

interface SetBalanceModalProps {
  currency: string;
  /** The account's current balance in minor units, used to pre-fill the input. */
  currentBalance: number;
  isOpen: boolean;
  onOpenChange(open: boolean): void;
  onSubmit(payload: SetAccountBalanceRequest): Promise<unknown>;
  isPending: boolean;
  error: Error | null;
}

/** Today's date as a `YYYY-MM-DD` string. */
function todayInput(): string {
  return new Date().toISOString().slice(0, 10);
}

/**
 * Modal for reconciling an account by setting its balance to a target value as of
 * a date. The server records a balance-adjustment transaction for the difference.
 */
export function SetBalanceModal({
  currency,
  currentBalance,
  isOpen,
  onOpenChange,
  onSubmit,
  isPending,
  error
}: SetBalanceModalProps) {
  const [asOf, setAsOf] = useState(todayInput());
  const [balance, setBalance] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setLocalError(null);
    setAsOf(todayInput());
    setBalance(String(minorToMajor(currentBalance, currency)));
  }, [isOpen, currentBalance, currency]);

  const handleSubmit = async () => {
    setLocalError(null);

    const minor = parseMoneyToMinor(balance, currency);

    if (minor === null) {
      setLocalError('Enter a valid balance.');
      return;
    }

    if (!asOf) {
      setLocalError('Select a date.');
      return;
    }

    await onSubmit({ asOf, balance: minor });
  };

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange}>
      <Modal.Backdrop>
        <Modal.Container size="sm">
          <Modal.Dialog>
            <Modal.CloseTrigger />
            <Modal.Header>
              <Modal.Heading>Update balance</Modal.Heading>
            </Modal.Header>
            <Modal.Body className="space-y-4">
              {error && <ApiErrorDisplay error={error} title="Update failed" showDetails={showErrorDetails} />}
              {localError && (
                <div className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-sm text-danger">
                  {localError}
                </div>
              )}

              <p className="text-sm text-muted">
                Set this account&apos;s balance as of a date. A balance-adjustment transaction is recorded for the
                difference.
              </p>

              <DateField label="As of" value={asOf} onChange={setAsOf} isRequired />

              <FormField
                label={`Balance (${currency.toUpperCase()})`}
                value={balance}
                onChange={setBalance}
                isRequired
                placeholder="0.00"
              />
            </Modal.Body>
            <Modal.Footer>
              <Button slot="close" variant="outline">
                Cancel
              </Button>
              <Button onPress={handleSubmit} isPending={isPending} isDisabled={!balance.trim()}>
                Update balance
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}
