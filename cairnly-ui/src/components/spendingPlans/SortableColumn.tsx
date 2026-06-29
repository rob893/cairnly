import { Table } from '@heroui/react';

interface SortableColumnProps {
  id: string;
  label: string;
  align?: 'left' | 'right';
  isRowHeader?: boolean;
}

/** Sortable HeroUI table column with a render-prop sort indicator. */
export function SortableColumn({ id, label, align = 'left', isRowHeader }: SortableColumnProps) {
  return (
    <Table.Column
      id={id}
      allowsSorting
      isRowHeader={isRowHeader}
      className={align === 'right' ? 'text-right' : undefined}
    >
      {({ sortDirection }: { sortDirection?: 'ascending' | 'descending' }) => (
        <Table.SortableColumnHeader
          sortDirection={sortDirection}
          className={align === 'right' ? 'justify-end gap-1' : undefined}
        >
          {label}
        </Table.SortableColumnHeader>
      )}
    </Table.Column>
  );
}
