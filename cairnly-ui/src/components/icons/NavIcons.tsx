interface IconProps {
  /** Classes controlling size and color (icons stroke/fill with `currentColor`). */
  className?: string;
}

const baseProps = {
  viewBox: '0 0 24 24',
  fill: 'none',
  stroke: 'currentColor',
  strokeWidth: 2,
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
  'aria-hidden': true
};

/** Dashboard / home icon (a simple house). */
export function DashboardIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M3 10.5 12 3l9 7.5" />
      <path d="M5 9.5V21h14V9.5" />
      <path d="M9.5 21v-6h5v6" />
    </svg>
  );
}

/** Budgets icon (stacked layers / plan). */
export function BudgetsIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="m12 3 9 5-9 5-9-5 9-5Z" />
      <path d="m3 13 9 5 9-5" />
      <path d="m3 18 9 5 9-5" opacity="0.6" />
    </svg>
  );
}

/** Account / user icon. */
export function AccountIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <circle cx="12" cy="8" r="4" />
      <path d="M4 21a8 8 0 0 1 16 0" />
    </svg>
  );
}

/** Sign-out icon. */
export function LogoutIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
      <path d="m16 17 5-5-5-5" />
      <path d="M21 12H9" />
    </svg>
  );
}

/** Hamburger / menu icon. */
export function MenuIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M3 6h18M3 12h18M3 18h18" />
    </svg>
  );
}

/** Chevron pointing up and down (select affordance). */
export function ChevronUpDownIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="m8 9 4-4 4 4" />
      <path d="m16 15-4 4-4-4" />
    </svg>
  );
}

/** Chevron pointing down (collapsible affordance). */
export function ChevronDownIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="m6 9 6 6 6-6" />
    </svg>
  );
}

/** Bank / accounts icon (columned building). */
export function BankIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="m3 9 9-5 9 5" />
      <path d="M4 9h16v1H4z" />
      <path d="M6 11v6M10 11v6M14 11v6M18 11v6" />
      <path d="M3 20h18" />
    </svg>
  );
}

/** Transactions icon (list with leading markers). */
export function TransactionsIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M8 6h12M8 12h12M8 18h12" />
      <path d="M3.5 6h.01M3.5 12h.01M3.5 18h.01" />
    </svg>
  );
}

/** Cash flow icon (bar chart). */
export function CashFlowIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M4 20V10M10 20V4M16 20v-7M22 20H2" />
    </svg>
  );
}

/** Search / magnifier icon. */
export function SearchIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <circle cx="11" cy="11" r="7" />
      <path d="m21 21-4.3-4.3" />
    </svg>
  );
}

/** Calendar / date icon. */
export function CalendarIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <rect x="3" y="4" width="18" height="17" rx="2" />
      <path d="M3 9h18M8 2v4M16 2v4" />
    </svg>
  );
}

/** Upward trend arrow (income / positive). */
export function TrendUpIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M3 17 9 11l4 4 8-8" />
      <path d="M21 11V7m0 0h-4" />
    </svg>
  );
}

/** Downward trend arrow (expenses / outflow). */
export function TrendDownIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M3 7 9 13l4-4 8 8" />
      <path d="M21 13v4m0 0h-4" />
    </svg>
  );
}

/** Wallet / remaining-balance icon. */
export function WalletIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M3 7a2 2 0 0 1 2-2h12a2 2 0 0 1 2 2v1" />
      <path d="M3 7v10a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7a2 2 0 0 0-2-2H5a2 2 0 0 1-2-2Z" />
      <circle cx="16.5" cy="13" r="1" />
    </svg>
  );
}

/** Plus / add icon. */
export function PlusIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M12 5v14M5 12h14" />
    </svg>
  );
}

/** Refresh / sync icon. */
export function RefreshIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M21 12a9 9 0 1 1-2.64-6.36" />
      <path d="M21 3v6h-6" />
    </svg>
  );
}

/** Filter / funnel icon. */
export function FilterIcon({ className }: IconProps) {
  return (
    <svg className={className} {...baseProps}>
      <path d="M3 5h18l-7 8v6l-4-2v-4L3 5Z" />
    </svg>
  );
}
