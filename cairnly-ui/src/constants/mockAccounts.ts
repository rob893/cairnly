/**
 * Placeholder data for the Accounts page. There is no accounts API yet, so this
 * module supplies static, realistic-looking values (all monetary amounts are in
 * integer **minor units** / cents to match {@link formatMoney}).
 */

/** A single financial account within a group. */
export interface MockAccount {
  id: string;
  name: string;
  /** Account type label (e.g. "Checking", "Credit Card"). */
  type: string;
  /** Current balance in minor units. */
  balance: number;
  /** Relative "last updated" label. */
  updated: string;
  /** Normalized sparkline points (any scale; rendered relatively). */
  trend: number[];
}

/** A named group of accounts (e.g. Cash, Credit Cards) with a period change. */
export interface MockAccountGroup {
  id: string;
  name: string;
  /** Group total balance in minor units. */
  total: number;
  /** Period change in minor units (positive or negative). */
  change: number;
  /** Period change as a percentage. */
  changePercent: number;
  accounts: MockAccount[];
}

/** A row in the assets/liabilities summary breakdown. */
export interface MockSummaryItem {
  label: string;
  /** Amount in minor units. */
  amount: number;
  /** Tailwind background class for the legend dot / bar segment. */
  color: string;
}

/** The current placeholder currency for all Accounts data. */
export const mockCurrency = 'USD';

/** Net worth headline figures. */
export const mockNetWorth = {
  total: 121_033_267,
  change: 75_687_112,
  changePercent: 166.9
};

/** Net worth time series for the area chart (value in major units). */
export const mockNetWorthSeries: ReadonlyArray<{ date: string; value: number }> = [
  { date: 'Jun 22', value: 453_461 },
  { date: 'Jun 22', value: 612_900 },
  { date: 'Jun 23', value: 820_140 },
  { date: 'Jun 23', value: 1_010_500 },
  { date: 'Jun 24', value: 1_205_300 },
  { date: 'Jun 24', value: 1_206_900 },
  { date: 'Jun 25', value: 1_210_333 }
];

/** Account groups shown on the Accounts page. */
export const mockAccountGroups: MockAccountGroup[] = [
  {
    id: 'cash',
    name: 'Cash',
    total: 8_386_708,
    change: 6_564_362,
    changePercent: 360.2,
    accounts: [
      {
        id: 'usaa-checking',
        name: 'USAA CLASSIC CHECKING (...0335)',
        type: 'Checking',
        balance: 1_815_825,
        updated: '2 hours ago',
        trend: [9, 8, 8, 7, 6, 5, 5]
      },
      {
        id: 'savings',
        name: 'Savings Account (...4512)',
        type: 'Savings',
        balance: 6_570_883,
        updated: '8 hours ago',
        trend: [5, 5, 5, 5, 5, 5, 5]
      },
      {
        id: 'personal-profile',
        name: 'Personal Profile',
        type: 'Checking',
        balance: 0,
        updated: '16 hours ago',
        trend: [2, 2, 1, 1, 1, 0, 0]
      }
    ]
  },
  {
    id: 'credit-cards',
    name: 'Credit Cards',
    total: 47_462,
    change: 30_789,
    changePercent: 184.7,
    accounts: [
      {
        id: 'primary-cc',
        name: 'Primary Credit Card (...6744)',
        type: 'Credit Card',
        balance: 18_948,
        updated: '2 hours ago',
        trend: [1, 1, 2, 3, 4, 5, 6]
      },
      {
        id: 'secondary-cc',
        name: 'Secondary Credit Card (...7924)',
        type: 'Credit Card',
        balance: 0,
        updated: '2 hours ago',
        trend: [3, 3, 3, 2, 2, 1, 0]
      },
      {
        id: 'chase-cc',
        name: 'CREDIT CARD (...2289)',
        type: 'Credit Card',
        balance: 28_514,
        updated: '17 hours ago',
        trend: [1, 2, 2, 3, 4, 5, 6]
      }
    ]
  },
  {
    id: 'investments',
    name: 'Investments',
    total: 96_500_110,
    change: 55_913_539,
    changePercent: 137.8,
    accounts: [
      {
        id: 'crypto',
        name: 'Crypto (...1611)',
        type: 'Cryptocurrency',
        balance: 93_267,
        updated: '9 hours ago',
        trend: [4, 6, 5, 7, 9, 8, 10]
      },
      {
        id: 'brokerage',
        name: 'Brokerage (...8820)',
        type: 'Taxable',
        balance: 50_412_300,
        updated: '3 hours ago',
        trend: [3, 4, 5, 6, 7, 9, 10]
      },
      {
        id: 'retirement',
        name: 'Roth IRA (...4471)',
        type: 'Retirement',
        balance: 45_994_543,
        updated: '3 hours ago',
        trend: [2, 3, 4, 5, 6, 8, 9]
      }
    ]
  }
];

/** Assets breakdown for the summary card. */
export const mockAssets: MockSummaryItem[] = [
  { label: 'Investments', amount: 96_500_110, color: 'bg-sky-400' },
  { label: 'Real Estate', amount: 50_240_000, color: 'bg-violet-400' },
  { label: 'Cash', amount: 8_386_708, color: 'bg-emerald-400' },
  { label: 'Vehicles', amount: 1_953_911, color: 'bg-amber-400' },
  { label: 'Valuables', amount: 1_000_000, color: 'bg-rose-400' }
];

/** Liabilities breakdown for the summary card. */
export const mockLiabilities: MockSummaryItem[] = [
  { label: 'Loans', amount: 37_000_000, color: 'bg-amber-400' },
  { label: 'Credit Cards', amount: 47_462, color: 'bg-rose-400' }
];
