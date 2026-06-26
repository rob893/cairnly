/**
 * Placeholder data for the Transactions page. No transactions API exists yet, so
 * these are static rows. Amounts are signed integer **minor units** (cents):
 * negative = money out, positive = money in.
 */

/** A single transaction row. */
export interface MockTransaction {
  id: string;
  /** Merchant / description. */
  name: string;
  /** Category label with a leading emoji glyph. */
  category: string;
  categoryEmoji: string;
  /** Source account label. */
  account: string;
  /** Signed amount in minor units (negative = outflow). */
  amount: number;
}

/** Transactions for a single day with the day's net total. */
export interface MockTransactionGroup {
  date: string;
  total: number;
  transactions: MockTransaction[];
}

/** The placeholder currency for all transactions. */
export const mockTransactionsCurrency = 'USD';

/** Transactions grouped by day, most recent first. */
export const mockTransactionGroups: MockTransactionGroup[] = [
  {
    date: 'June 24, 2026',
    total: -8534,
    transactions: [
      {
        id: 'att',
        name: 'AT&T',
        category: 'Internet & Cable',
        categoryEmoji: '🌐',
        account: 'USAA CLASSIC CHECKING (...0335)',
        amount: -6521
      },
      {
        id: 'amazon',
        name: 'Amazon',
        category: 'Shopping',
        categoryEmoji: '🛍️',
        account: 'CREDIT CARD (...2289)',
        amount: -2013
      },
      {
        id: 'vanguard-buy',
        name: 'Buy 0.257922 Shares of Vanguard S&p 500 Etf for $678.50 Each Purchased',
        category: 'Buy',
        categoryEmoji: '📈',
        account: 'Robinhood individual (...3870)',
        amount: -17500
      }
    ]
  },
  {
    date: 'June 23, 2026',
    total: 62099,
    transactions: [
      {
        id: 'vt-reinvest',
        name: 'Dividend Reinvestment Purchase of 0.049 Shares of Vt for $7.58 Total',
        category: 'Buy',
        categoryEmoji: '📈',
        account: 'Robinhood Roth IRA (...5249)',
        amount: -758
      },
      {
        id: 'vt-dividend',
        name: 'Cash Dividend of $7.58 From Vt Dividend',
        category: 'Dividends & Capital Gains',
        categoryEmoji: '🟦',
        account: 'Robinhood Roth IRA (...5249)',
        amount: 758
      },
      {
        id: 'vt-reinvest-2',
        name: 'Dividend Reinvestment Purchase of 1.118 Shares of Vt for $172.60 Total',
        category: 'Buy',
        categoryEmoji: '📈',
        account: 'Robinhood individual (...3870)',
        amount: -17260
      },
      {
        id: 'vt-dividend-2',
        name: 'Cash Dividend of $172.60 From Vt Dividend',
        category: 'Dividends & Capital Gains',
        categoryEmoji: '🟦',
        account: 'Robinhood individual (...3870)',
        amount: 17260
      },
      {
        id: 'vxus-reinvest',
        name: 'Dividend Reinvestment Purchase of 0.275 Shares of Vxus for $23.22 Total',
        category: 'Buy',
        categoryEmoji: '📈',
        account: 'Robinhood individual (...3870)',
        amount: -2322
      },
      {
        id: 'vxus-dividend',
        name: 'Cash Dividend of $23.22 From Vxus Dividend',
        category: 'Dividends & Capital Gains',
        categoryEmoji: '🟦',
        account: 'Robinhood individual (...3870)',
        amount: 2322
      },
      {
        id: 'hsa-dividend',
        name: 'Dividend Received Vanguard Intl Equity Index FDS Tt Wr... (Vt) (Cash)',
        category: 'Dividends & Capital Gains',
        categoryEmoji: '🟦',
        account: 'Health Savings Account (...2966)',
        amount: 141
      },
      {
        id: 'hsa-purchase',
        name: 'Purchase Into Core Account Fidelity Government Cash Reserves (Fdrxx)',
        category: 'Uncategorized',
        categoryEmoji: '❓',
        account: 'Health Savings Account (...2966)',
        amount: -141
      },
      {
        id: 'vxus-received',
        name: 'Dividend Received Vanguard Total International Stock I... (Vxus) (Cash)',
        category: 'Dividends & Capital Gains',
        categoryEmoji: '🟦',
        account: 'Individual (...7771)',
        amount: 12712
      },
      {
        id: 'vt-received',
        name: 'Dividend Received Vanguard Intl Equity Index FDS Tt Wr... (Vt) (Cash)',
        category: 'Dividends & Capital Gains',
        categoryEmoji: '🟦',
        account: 'Individual (...7771)',
        amount: 29047
      },
      {
        id: 'core-transfer',
        name: 'Purchase Into Core Account Cash (315994103) (Cash)',
        category: 'Transfer',
        categoryEmoji: '🔁',
        account: 'Individual (...7771)',
        amount: -41759
      }
    ]
  }
];
