import { useState } from 'react';
import { Link } from 'react-router';
import { Button } from '@heroui/react';
import { formatMoney } from '../../utils/money';
import { formatRelativeTime } from '../../utils/datetime';
import { ChevronDown, Pencil, TrendingDown, TrendingUp, Trash2 } from 'lucide-react';
import { Sparkline } from './Sparkline';
import type { Account } from '../../types/accounts';
import type { AccountGroupView } from '../../utils/accounts';

interface AccountGroupProps {
  group: AccountGroupView;
  currency: string;
  onEdit(account: Account): void;
  onDelete(account: Account): void;
}

/**
 * A collapsible group of accounts of the same type with its window change in the
 * header and a list of member accounts, each showing a balance trend sparkline and
 * inline edit/delete actions.
 */
export function AccountGroup({ group, currency, onEdit, onDelete }: AccountGroupProps) {
  const [open, setOpen] = useState(true);
  const positive = group.change >= 0;

  return (
    <section className="rounded-2xl border border-border bg-surface">
      <button
        type="button"
        onClick={() => setOpen(prev => !prev)}
        aria-expanded={open}
        className="flex w-full items-center gap-3 px-5 py-4 text-left outline-none focus-visible:ring-2 focus-visible:ring-focus"
      >
        <ChevronDown className={`size-4 shrink-0 text-muted transition-transform ${open ? '' : '-rotate-90'}`} />
        <span className="font-semibold text-foreground">{group.label}</span>
        {group.change !== 0 && (
          <span className={`flex items-center gap-1 text-sm font-medium ${positive ? 'text-success' : 'text-danger'}`}>
            {positive ? <TrendingUp className="size-3.5" /> : <TrendingDown className="size-3.5" />}
            {formatMoney(group.change, currency)} ({group.changePercent.toFixed(1)}%)
          </span>
        )}
        <span className="ml-auto text-lg font-semibold tabular-nums text-foreground">
          {formatMoney(group.total, currency)}
        </span>
      </button>

      {open && (
        <ul className="divide-y divide-border border-t border-border">
          {group.accounts.map(row => (
            <li
              key={row.account.id}
              className="flex items-center gap-4 px-5 py-3 transition-colors hover:bg-surface-secondary/40"
            >
              <Link
                to={`/accounts/${row.account.id}`}
                className="flex min-w-0 flex-1 items-center gap-4 no-underline outline-none focus-visible:ring-2 focus-visible:ring-focus"
              >
                <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-surface-secondary text-sm font-semibold text-muted">
                  {row.account.name[0]?.toUpperCase()}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium text-foreground">{row.account.name}</p>
                  <p className="text-xs text-muted">
                    {row.typeLabel}
                    {row.account.isManual ? ' · Manual' : ''}
                  </p>
                </div>
              </Link>
              {row.trend.length > 1 && <Sparkline points={row.trend} className="hidden shrink-0 sm:block" />}
              <div className="w-28 shrink-0 text-right">
                <p className="font-semibold tabular-nums text-foreground">{formatMoney(row.balance, currency)}</p>
                <p className="text-xs text-muted">{formatRelativeTime(row.account.updatedAt) || '—'}</p>
              </div>
              <div className="flex shrink-0 items-center gap-1">
                <Button
                  isIconOnly
                  variant="ghost"
                  size="sm"
                  onPress={() => onEdit(row.account)}
                  aria-label={`Edit ${row.account.name}`}
                >
                  <Pencil className="size-4" />
                </Button>
                <Button
                  isIconOnly
                  variant="danger-soft"
                  size="sm"
                  onPress={() => onDelete(row.account)}
                  aria-label={`Delete ${row.account.name}`}
                >
                  <Trash2 className="size-4" />
                </Button>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
