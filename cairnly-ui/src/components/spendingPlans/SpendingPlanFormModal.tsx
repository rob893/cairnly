import { useEffect, useState } from 'react';
import { Button, Label, Modal, TextArea, TextField } from '@heroui/react';
import { FormField } from '../FormField';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import type { SpendingPlan, CreateSpendingPlanRequest } from '../../types/spendingPlans';

interface SpendingPlanFormModalProps {
  isOpen: boolean;
  onOpenChange(open: boolean): void;
  /** The spendingPlan being edited, or `undefined` when creating a new one. */
  spendingPlan?: SpendingPlan;
  onSubmit(payload: CreateSpendingPlanRequest): Promise<unknown>;
  isPending: boolean;
  error: Error | null;
}

/** Modal form for creating or editing a spendingPlan's name, currency, and description. */
export function SpendingPlanFormModal({
  isOpen,
  onOpenChange,
  spendingPlan,
  onSubmit,
  isPending,
  error
}: SpendingPlanFormModalProps) {
  const isEdit = spendingPlan !== undefined;

  const [name, setName] = useState('');
  const [currency, setCurrency] = useState('USD');
  const [description, setDescription] = useState('');
  const [localError, setLocalError] = useState<string | null>(null);

  useEffect(() => {
    if (!isOpen) {
      return;
    }

    setLocalError(null);
    setName(spendingPlan?.name ?? '');
    setCurrency(spendingPlan?.currency ?? 'USD');
    setDescription(spendingPlan?.description ?? '');
  }, [isOpen, spendingPlan]);

  const handleSubmit = async () => {
    setLocalError(null);

    if (!name.trim()) {
      setLocalError('Name is required.');
      return;
    }

    if (!/^[A-Za-z]{3}$/.test(currency.trim())) {
      setLocalError('Currency must be a 3-letter ISO 4217 code (e.g. USD).');
      return;
    }

    await onSubmit({
      name: name.trim(),
      currency: currency.trim().toUpperCase(),
      description: description.trim() ? description.trim() : null
    });
  };

  return (
    <Modal isOpen={isOpen} onOpenChange={onOpenChange}>
      <Modal.Backdrop>
        <Modal.Container size="md">
          <Modal.Dialog>
            <Modal.CloseTrigger />
            <Modal.Header>
              <Modal.Heading>{isEdit ? 'Edit spending plan' : 'New spending plan'}</Modal.Heading>
            </Modal.Header>
            <Modal.Body className="space-y-4">
              {error && <ApiErrorDisplay error={error} title="Save failed" showDetails={showErrorDetails} />}
              {localError && (
                <div className="rounded-lg border border-danger/20 bg-danger/10 p-3 text-sm text-danger">
                  {localError}
                </div>
              )}

              <FormField label="Name" value={name} onChange={setName} isRequired placeholder="e.g. Household 2026" />

              <FormField
                label="Currency"
                value={currency}
                onChange={setCurrency}
                isRequired
                placeholder="USD"
                description="3-letter ISO 4217 code."
                className="w-full sm:w-40"
              />

              <TextField value={description} onChange={setDescription} className="w-full">
                <Label className="mb-1 block text-sm">Description</Label>
                <TextArea
                  placeholder="Optional notes"
                  className="w-full min-h-20 rounded-lg border border-border bg-surface px-3 py-2 text-sm"
                />
              </TextField>
            </Modal.Body>
            <Modal.Footer>
              <Button slot="close" variant="outline">
                Cancel
              </Button>
              <Button onPress={handleSubmit} isPending={isPending} isDisabled={!name.trim()}>
                {isEdit ? 'Save changes' : 'Create spending plan'}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}
