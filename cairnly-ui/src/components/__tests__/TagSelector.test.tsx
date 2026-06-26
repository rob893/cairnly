import { describe, it, expect, vi, beforeAll } from 'vitest';
import { render, screen } from '@testing-library/react';
import { TagSelector } from '../spendingPlans/TagSelector';
import type { Tag } from '../../types/tags';

const sampleTags: Tag[] = [
  { id: 1, userId: 1, name: 'subscription', metadata: {}, createdAt: '', updatedAt: '' },
  { id: 2, userId: 1, name: 'streaming', metadata: {}, createdAt: '', updatedAt: '' },
  { id: 3, userId: 1, name: 'housing', metadata: {}, createdAt: '', updatedAt: '' }
];

vi.mock('../../hooks/tags', () => ({
  useTags: () => ({
    tags: sampleTags,
    tagsById: new Map(sampleTags.map(t => [t.id, t])),
    isLoading: false
  }),
  useCreateTag: () => ({ mutateAsync: vi.fn(), isPending: false })
}));

beforeAll(() => {
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

describe('TagSelector', () => {
  it('renders selected tags as chips and other tags as suggestions', () => {
    render(<TagSelector value={[1]} onChange={() => {}} />);

    // Selected tag chip.
    expect(screen.getByText('subscription')).toBeInTheDocument();
    // Remove control for the selected tag.
    expect(screen.getByRole('button', { name: 'Remove subscription' })).toBeInTheDocument();
    // Unselected tags appear as suggestion buttons.
    expect(screen.getByRole('button', { name: 'streaming' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'housing' })).toBeInTheDocument();
  });

  it('renders an input placeholder prompting for tags', () => {
    render(<TagSelector value={[]} onChange={() => {}} />);
    expect(screen.getByPlaceholderText(/Add tags/i)).toBeInTheDocument();
  });
});
