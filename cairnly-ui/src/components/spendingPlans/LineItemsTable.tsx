import { memo, type ReactNode } from 'react';
import type { SortDescriptor } from 'react-aria-components';
import { Button, Chip, Table } from '@heroui/react';
import { Eye, Pencil, Trash2 } from 'lucide-react';
import { EditableCategoryCell } from '../EditableCategoryCell';
import { formatMoney } from '../../utils/money';
import { cadenceBreakdown } from '../../utils/cadence';
import { cadenceLabel } from '../../types/spendingPlans';
import type { Category } from '../../types/categories';
import type { Tag } from '../../types/tags';
import { EditableNameCell } from './EditableNameCell';
import { SortableColumn } from './SortableColumn';
import { COST_COLUMNS, type LineItem, type LineItemInlinePatch } from './lineItemTypes';

interface LineItemsTableProps {
  title: string;
  kind: 'income' | 'expense';
  isIncome: boolean;
  currency: string;
  items: LineItem[];
  searchTerm: string;
  sortKey: string;
  sortDirection: 'asc' | 'desc';
  onSortChange(key: string, direction: 'asc' | 'desc'): void;
  categoriesById: Map<number, Category>;
  tagsById: Map<number, Tag>;
  onDetails(item: LineItem): void;
  onEdit(item: LineItem): void;
  onDelete(item: LineItem): void;
  onInlineSave(item: LineItem, patch: LineItemInlinePatch): Promise<void>;
}

/** Sortable HeroUI table for spending-plan income and expense line items. */
export const LineItemsTable = memo(function LineItemsTable({
  title,
  kind,
  isIncome,
  currency,
  items,
  searchTerm,
  sortKey,
  sortDirection,
  onSortChange,
  categoriesById,
  tagsById,
  onDetails,
  onEdit,
  onDelete,
  onInlineSave
}: LineItemsTableProps) {
  const sortDescriptor: SortDescriptor = {
    column: sortKey,
    direction: sortDirection === 'asc' ? 'ascending' : 'descending'
  };

  function handleSortChange(descriptor: SortDescriptor): void {
    onSortChange(String(descriptor.column), descriptor.direction === 'descending' ? 'desc' : 'asc');
  }

  function renderCells(item: LineItem): ReactNode[] {
    const breakdown = cadenceBreakdown(item.amount, item.cadence);
    const tagNames = item.tagIds.map(id => tagsById.get(id)?.name ?? '').filter(name => name !== '');
    const category = categoriesById.get(item.categoryId);

    const cells = [
      <Table.Cell key="details">
        <Button
          isIconOnly
          variant="ghost"
          size="sm"
          aria-label={`View ${item.name} details`}
          onPress={() => onDetails(item)}
        >
          <Eye className="size-4" />
        </Button>
      </Table.Cell>,
      <Table.Cell key="name">
        <EditableNameCell item={item} onSave={onInlineSave} />
      </Table.Cell>,
      <Table.Cell key="category">
        <EditableCategoryCell
          item={item}
          categoryId={item.categoryId}
          category={category}
          categoryKind={kind === 'income' ? 'Income' : 'Expense'}
          editAriaLabel={`Edit ${item.name} category`}
          buttonClassName="-mx-1 flex max-w-full items-center gap-1.5 rounded px-1 text-left transition-colors hover:bg-surface-secondary focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-focus"
          onSave={(lineItem, categoryId) => onInlineSave(lineItem, { categoryId })}
        />
      </Table.Cell>,
      <Table.Cell key="tags">
        {tagNames.length === 0 ? (
          <span className="text-muted">—</span>
        ) : (
          <span className="flex flex-wrap gap-1">
            {tagNames.map(name => (
              <Chip key={name} variant="soft" size="sm">
                {name}
              </Chip>
            ))}
          </span>
        )}
      </Table.Cell>
    ];

    if (isIncome) {
      cells.push(
        <Table.Cell key="amount" className="text-right font-semibold tabular-nums text-success">
          {formatMoney(item.amount, currency)}
        </Table.Cell>
      );
    } else {
      cells.push(
        <Table.Cell key="cadence">
          <Chip variant="soft" size="sm">
            {cadenceLabel(item.cadence)}
          </Chip>
        </Table.Cell>,
        ...COST_COLUMNS.map(col => (
          <Table.Cell key={col.key} className="text-right tabular-nums">
            {formatMoney(breakdown[col.key], currency)}
          </Table.Cell>
        ))
      );
    }

    cells.push(
      <Table.Cell key="actions">
        <div className="flex items-center justify-end gap-1">
          <Button isIconOnly variant="ghost" size="sm" onPress={() => onEdit(item)} aria-label={`Edit ${item.name}`}>
            <Pencil className="size-4" />
          </Button>
          <Button
            isIconOnly
            variant="danger-soft"
            size="sm"
            onPress={() => onDelete(item)}
            aria-label={`Delete ${item.name}`}
          >
            <Trash2 className="size-4" />
          </Button>
        </div>
      </Table.Cell>
    );

    return cells;
  }

  return (
    <Table aria-label={`${title} line items`}>
      <Table.ScrollContainer>
        <Table.Content
          aria-label={`${title} line items`}
          sortDescriptor={sortDescriptor}
          onSortChange={handleSortChange}
          className="min-w-[40rem]"
        >
          <Table.Header>
            <Table.Column id="details" width={48} aria-label="Details">
              {''}
            </Table.Column>
            <SortableColumn id="name" label="Name" isRowHeader />
            <SortableColumn id="category" label="Category" />
            <Table.Column id="tags">Tags</Table.Column>
            {isIncome ? (
              <SortableColumn id="amount" label="Amount" align="right" />
            ) : (
              <>
                <SortableColumn id="cadence" label="Frequency" />
                {COST_COLUMNS.map(col => (
                  <SortableColumn key={col.key} id={col.key} label={col.label} align="right" />
                ))}
              </>
            )}
            <Table.Column id="actions" className="text-right">
              Actions
            </Table.Column>
          </Table.Header>
          <Table.Body
            items={items}
            renderEmptyState={() => (
              <span className="block py-8 text-center text-sm text-muted">
                No {kind} matches “{searchTerm}”.
              </span>
            )}
          >
            {(item: LineItem) => <Table.Row id={item.id}>{renderCells(item)}</Table.Row>}
          </Table.Body>
        </Table.Content>
      </Table.ScrollContainer>
    </Table>
  );
});
