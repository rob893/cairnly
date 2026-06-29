import { Button, Chip, Modal } from '@heroui/react';
import { formatMoney } from '../../utils/money';
import { cadenceLabel } from '../../types/spendingPlans';
import type { LineItem } from './lineItemTypes';

interface DetailsModalProps {
  item: LineItem | undefined;
  currency: string;
  tagNames: string[];
  onClose(): void;
  onTagClick(name: string): void;
}

/** Modal showing the selected line item's description, tags, and cadence-adjusted amount. */
export function DetailsModal({ item, currency, tagNames, onClose, onTagClick }: DetailsModalProps) {
  return (
    <Modal isOpen={item !== undefined} onOpenChange={open => !open && onClose()}>
      <Modal.Backdrop>
        <Modal.Container size="sm">
          <Modal.Dialog>
            <Modal.CloseTrigger />
            <Modal.Header>
              <Modal.Heading>{item?.name}</Modal.Heading>
            </Modal.Header>
            <Modal.Body className="space-y-4 text-sm">
              {item && (
                <>
                  {item.description ? (
                    <p className="text-muted">{item.description}</p>
                  ) : (
                    <p className="text-muted italic">No description.</p>
                  )}
                  <div>
                    <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-muted">Tags</p>
                    {tagNames.length === 0 ? (
                      <span className="text-xs text-muted">None</span>
                    ) : (
                      <div className="flex flex-wrap gap-2">
                        {tagNames.map(name => (
                          <button key={name} type="button" onClick={() => onTagClick(name)} className="no-underline">
                            <Chip variant="soft" size="sm">
                              {name}
                            </Chip>
                          </button>
                        ))}
                      </div>
                    )}
                  </div>
                  <div>
                    <p className="mb-1 text-xs font-semibold uppercase tracking-wide text-muted">Amount</p>
                    <p>
                      {formatMoney(item.amount, currency)} per {cadenceLabel(item.cadence).toLowerCase()} period
                    </p>
                  </div>
                </>
              )}
            </Modal.Body>
            <Modal.Footer>
              <Button slot="close" variant="outline">
                Close
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}
