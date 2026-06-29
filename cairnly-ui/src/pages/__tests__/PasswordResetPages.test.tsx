import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ForgotPasswordPage } from '../ForgotPasswordPage';
import { ResetPasswordPage } from '../ResetPasswordPage';
import { authApi } from '../../services/auth';

vi.mock('../../services/auth', () => ({
  authApi: {
    forgotPassword: vi.fn(),
    resetPassword: vi.fn()
  }
}));

function renderWithRoute(element: React.ReactNode, initialEntry = '/') {
  return render(
    <MemoryRouter initialEntries={[initialEntry]}>
      <Routes>
        <Route path="/" element={element} />
        <Route path="/forgot-password" element={element} />
        <Route path="/reset-password" element={element} />
        <Route path="/login" element={<div>Login Page</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('ForgotPasswordPage', () => {
  beforeEach(() => {
    vi.mocked(authApi.forgotPassword).mockReset();
  });

  it('uses the branded AuthShell instead of the legacy standalone card', () => {
    const { container } = renderWithRoute(<ForgotPasswordPage />);

    expect(screen.getByRole('link', { name: /cairnly/i })).toHaveAttribute('href', '/');
    expect(container.innerHTML).not.toMatch(/to-content1|text-primary|text-default-/);
  });

  it('shows the submitted state with a lucide icon instead of emoji', async () => {
    vi.mocked(authApi.forgotPassword).mockResolvedValue();
    const user = userEvent.setup();
    const { container } = renderWithRoute(<ForgotPasswordPage />);

    await user.type(screen.getByLabelText(/email address/i), 'person@example.com');
    await user.click(screen.getByRole('button', { name: /send reset link/i }));

    await waitFor(() => expect(screen.getByText('Email Sent')).toBeInTheDocument());
    expect(container.textContent).not.toContain('✅');
  });
});

describe('ResetPasswordPage', () => {
  beforeEach(() => {
    vi.mocked(authApi.resetPassword).mockReset();
  });

  it('uses the branded AuthShell and theme tokens for invalid reset links', () => {
    const { container } = renderWithRoute(<ResetPasswordPage />, '/reset-password');

    expect(screen.getByRole('link', { name: /cairnly/i })).toHaveAttribute('href', '/');
    expect(container.textContent).not.toContain('❌');
    expect(container.innerHTML).not.toMatch(/to-content1|text-primary|text-default-|bg-default-/);
  });
});
