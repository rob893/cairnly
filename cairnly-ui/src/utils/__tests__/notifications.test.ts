import { toast } from '@heroui/react';
import { describe, expect, it, vi } from 'vitest';
import { showSuccessToast } from '../notifications';

vi.mock('@heroui/react', () => ({
  ToastProvider: ({ children }: { children: React.ReactNode }) => children,
  toast: {
    success: vi.fn()
  }
}));

describe('showSuccessToast', () => {
  it('uses the global HeroUI success toast with calm defaults', () => {
    showSuccessToast('Transaction saved');

    expect(toast.success).toHaveBeenCalledWith('Transaction saved', { timeout: 3000 });
  });
});
