import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Plus } from 'lucide-react';
import { describe, expect, it, vi } from 'vitest';
import { EmptyState } from '../EmptyState';

describe('EmptyState', () => {
  it('renders an icon, title, subtitle, and optional CTA', async () => {
    const onPress = vi.fn();
    const user = userEvent.setup();

    render(
      <EmptyState
        icon={<Plus className="size-4" />}
        title="No transactions yet"
        subtitle="Add your first transaction to start tracking cash flow."
        cta={{ label: 'Add transaction', onPress }}
      />
    );

    expect(screen.getByText('No transactions yet')).toBeInTheDocument();
    expect(screen.getByText('Add your first transaction to start tracking cash flow.')).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Add transaction' }));

    expect(onPress).toHaveBeenCalledOnce();
  });

  it('omits the CTA when no action is supplied', () => {
    render(
      <EmptyState
        icon={<Plus className="size-4" />}
        title="No accounts yet"
        subtitle="Add an account to start tracking net worth."
      />
    );

    expect(screen.queryByRole('button')).not.toBeInTheDocument();
  });
});
