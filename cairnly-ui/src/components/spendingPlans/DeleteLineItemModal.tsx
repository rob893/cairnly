import { Button, Modal } from '@heroui/react';
import { ApiErrorDisplay } from '../ApiErrorDisplay';
import { showErrorDetails } from '../../utils/environment';
import type { LineItem } from './lineItemTypes';

interface DeleteLineItemModalProps {
  kind: 'income' | 'expense';
  item: LineItem | undefined;
  error: Error | null;
  isPending: boolean;
  onClose(): void;
  onDelete(): void;
}

/** Confirmation modal for deleting a spending-plan line item. */
export function DeleteLineItemModal({ kind, item, error, isPending, onClose, onDelete }: DeleteLineItemModalProps) {
  return (
    <Modal isOpen={item !== undefined} onOpenChange={open => !open && onClose()}>
      <Modal.Backdrop>
        <Modal.Container size="sm">
          <Modal.Dialog>
            <Modal.CloseTrigger />
            <Modal.Header>
              <Modal.Heading>Delete {kind}?</Modal.Heading>
            </Modal.Header>
            <Modal.Body className="space-y-4">
              {error && <ApiErrorDisplay error={error} title="Delete failed" showDetails={showErrorDetails} />}
              <p className="text-sm text-muted">
                Permanently delete <span className="font-medium text-foreground">{item?.name}</span>? This cannot be
                undone.
              </p>
            </Modal.Body>
            <Modal.Footer>
              <Button slot="close" variant="outline">
                Cancel
              </Button>
              <Button variant="danger" onPress={onDelete} isPending={isPending}>
                Delete
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}
