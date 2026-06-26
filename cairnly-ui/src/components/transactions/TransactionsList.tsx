import { ChevronDownIcon } from '../icons/NavIcons';
import { formatMoney } from '../../utils/money';
import { mockTransactionGroups, mockTransactionsCurrency } from '../../constants/mockTransactions';

/** Formats a signed minor-unit amount: inflows are prefixed with `+` and tinted. */
function amountDisplay(amount: number, currency: string): { text: string; className: string } {
  if (amount > 0) {
    return { text: `+${formatMoney(amount, currency)}`, className: 'text-success' };
  }

  return { text: formatMoney(Math.abs(amount), currency), className: 'text-foreground' };
}

/**
 * A read-only list of placeholder transactions grouped by day, each group headed
 * by its date and net total, mirroring the Transactions reference layout.
 */
export function TransactionsList() {
  const currency = mockTransactionsCurrency;

  return (
    <div className="overflow-hidden rounded-2xl border border-border bg-surface">
      {mockTransactionGroups.map(group => {
        const groupTotal = amountDisplay(group.total, currency);

        return (
          <section key={group.date}>
            <div className="flex items-center justify-between gap-3 bg-surface-secondary/60 px-4 py-2 text-sm">
              <span className="font-medium text-muted">{group.date}</span>
              <span className={`tabular-nums ${groupTotal.className}`}>{groupTotal.text}</span>
            </div>

            <ul className="divide-y divide-border">
              {group.transactions.map(txn => {
                const amount = amountDisplay(txn.amount, currency);

                return (
                  <li
                    key={txn.id}
                    className="grid grid-cols-[1fr_auto] items-center gap-x-4 gap-y-1 px-4 py-3 transition-colors hover:bg-surface-secondary/40 lg:grid-cols-[2fr_1fr_1fr_auto]"
                  >
                    <div className="flex min-w-0 items-center gap-3">
                      <span
                        className="flex size-8 shrink-0 items-center justify-center rounded-full bg-surface-secondary text-sm"
                        aria-hidden="true"
                      >
                        {txn.categoryEmoji}
                      </span>
                      <span className="truncate font-medium text-foreground">{txn.name}</span>
                    </div>

                    <div className="hidden min-w-0 items-center gap-2 text-sm text-muted lg:flex">
                      <span aria-hidden="true">{txn.categoryEmoji}</span>
                      <span className="truncate">{txn.category}</span>
                    </div>

                    <div className="hidden min-w-0 items-center gap-2 text-sm text-muted lg:flex">
                      <span className="flex size-5 shrink-0 items-center justify-center rounded-full bg-surface-secondary text-[10px] font-semibold">
                        {txn.account[0]?.toUpperCase()}
                      </span>
                      <span className="truncate">{txn.account}</span>
                    </div>

                    <div className="flex items-center justify-end gap-2">
                      <span className={`text-sm font-semibold tabular-nums ${amount.className}`}>{amount.text}</span>
                      <ChevronDownIcon className="size-4 -rotate-90 text-muted" />
                    </div>
                  </li>
                );
              })}
            </ul>
          </section>
        );
      })}
    </div>
  );
}
