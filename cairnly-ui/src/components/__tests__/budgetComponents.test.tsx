import { describe, it, expect, beforeAll } from 'vitest';
import { render, screen } from '@testing-library/react';
import { SelectField } from '../SelectField';
import { BudgetSummaryCard } from '../budgets/BudgetSummaryCard';
import type { BudgetSummary } from '../../types/budgets';

beforeAll(() => {
  // HeroUI's Select/ListBox (react-aria) relies on these browser APIs jsdom lacks.
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
      onchange: null
    })
  });
  globalThis.ResizeObserver = class {
    observe() {}
    unobserve() {}
    disconnect() {}
  };
});

describe('SelectField', () => {
  it('renders the label and the selected option value', () => {
    render(
      <SelectField
        label="Cadence"
        value="Monthly"
        onChange={() => {}}
        options={[
          { value: 'Weekly', label: 'Weekly' },
          { value: 'Monthly', label: 'Monthly' }
        ]}
      />
    );

    expect(screen.getByText('Cadence')).toBeInTheDocument();
    // Select.Value renders the currently selected option's label inside the trigger.
    expect(screen.getByText('Monthly')).toBeInTheDocument();
  });
});

describe('BudgetSummaryCard', () => {
  const summary: BudgetSummary = {
    budgetId: 1,
    currency: 'USD',
    income: { daily: 10000, weekly: 70000, monthly: 300000, annual: 3650000 },
    expenses: { daily: 5000, weekly: 35000, monthly: 150000, annual: 1825000 },
    remaining: { daily: 5000, weekly: 35000, monthly: 150000, annual: 1825000 }
  };

  it('renders income, expenses, and remaining rows with formatted currency', () => {
    render(<BudgetSummaryCard summary={summary} currency="USD" isLoading={false} />);

    expect(screen.getByText('Income')).toBeInTheDocument();
    expect(screen.getByText('Expenses')).toBeInTheDocument();
    expect(screen.getByText('Remaining')).toBeInTheDocument();
    // Daily income 10000 minor units => $100.00.
    expect(screen.getByText('$100.00')).toBeInTheDocument();
  });

  it('shows a spinner while loading', () => {
    const { container } = render(<BudgetSummaryCard currency="USD" isLoading={true} />);
    expect(container.querySelector('svg')).toBeInTheDocument();
  });
});
