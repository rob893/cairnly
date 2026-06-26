import { useState } from 'react';
import { formatMoney } from '../../utils/money';
import { ChevronDown, TrendingDown, TrendingUp } from 'lucide-react';
import { Sparkline } from './Sparkline';
import type { MockAccountGroup } from '../../constants/mockAccounts';

interface AccountGroupProps {
  group: MockAccountGroup;
  currency: string;
}

/**
 * A collapsible group of accounts (e.g. Cash) with its period change in the
 * header and a list of member accounts, each showing a balance trend sparkline.
 */
export function AccountGroup({ group, currency }: AccountGroupProps) {
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
        <span className="font-semibold text-foreground">{group.name}</span>
        <span className={`flex items-center gap-1 text-sm font-medium ${positive ? 'text-success' : 'text-danger'}`}>
          {positive ? <TrendingUp className="size-3.5" /> : <TrendingDown className="size-3.5" />}
          {formatMoney(group.change, currency)} ({group.changePercent.toFixed(1)}%)
        </span>
        <span className="ml-auto text-lg font-semibold tabular-nums text-foreground">
          {formatMoney(group.total, currency)}
        </span>
      </button>

      {open && (
        <ul className="divide-y divide-border border-t border-border">
          {group.accounts.map(account => (
            <li key={account.id} className="flex items-center gap-4 px-5 py-3">
              <div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-surface-secondary text-sm font-semibold text-muted">
                {account.name[0]?.toUpperCase()}
              </div>
              <div className="min-w-0 flex-1">
                <p className="truncate font-medium text-foreground">{account.name}</p>
                <p className="text-xs text-muted">{account.type}</p>
              </div>
              <Sparkline points={account.trend} className="hidden shrink-0 sm:block" />
              <div className="w-32 shrink-0 text-right">
                <p className="font-semibold tabular-nums text-foreground">{formatMoney(account.balance, currency)}</p>
                <p className="text-xs text-muted">{account.updated}</p>
              </div>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
