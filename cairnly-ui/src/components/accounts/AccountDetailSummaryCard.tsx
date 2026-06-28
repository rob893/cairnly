import { Card, CardContent, CardHeader } from '@heroui/react';
import { formatMoney } from '../../utils/money';
import { formatRelativeTime } from '../../utils/datetime';
import { accountClassLabel, accountTypeLabel } from '../../types/accounts';
import type { Account } from '../../types/accounts';

interface AccountDetailSummaryCardProps {
  account: Account;
  /** Number of transactions loaded for the account, or `undefined` while loading. */
  transactionCount: number | undefined;
}

/** A labeled key/value row in the summary card. */
function Row({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-3 text-sm">
      <span className="text-muted">{label}</span>
      <span className="font-medium tabular-nums text-foreground">{value}</span>
    </div>
  );
}

/** A read-only summary of an account's key attributes shown on the detail page. */
export function AccountDetailSummaryCard({ account, transactionCount }: AccountDetailSummaryCardProps) {
  return (
    <Card className="bg-surface border border-border">
      <CardHeader className="px-6 pt-6">
        <h2 className="text-lg font-semibold">Summary</h2>
      </CardHeader>
      <CardContent className="space-y-3 px-6 pb-6">
        <Row label="Account type" value={accountTypeLabel[account.type]} />
        <Row label="Class" value={accountClassLabel[account.class]} />
        <Row label="Currency" value={account.currency.toUpperCase()} />
        <Row label="Balance" value={account.isManual ? 'Manual' : 'From transactions'} />
        <Row label="Opening balance" value={formatMoney(account.openingBalance, account.currency)} />
        <Row label="Current balance" value={formatMoney(account.currentBalance, account.currency)} />
        <Row label="Total transactions" value={transactionCount !== undefined ? String(transactionCount) : '—'} />
        <Row label="Created" value={formatRelativeTime(account.createdAt) || '—'} />
      </CardContent>
    </Card>
  );
}
