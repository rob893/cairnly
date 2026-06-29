import React, { useState, useEffect } from 'react';
import { useNavigate, useSearchParams, Link } from 'react-router';
import { Button, Chip, Spinner } from '@heroui/react';
import { AlertCircle, MailCheck } from 'lucide-react';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { showErrorDetails } from '../utils/environment';
import { AuthShell } from '../components/AuthShell';
import { FormField } from '../components/FormField';
import { authApi } from '../services/auth';
import {
  validatePassword,
  getPasswordRequirementsDescription,
  type PasswordValidationResult
} from '../utils/passwordValidation';

export function ResetPasswordPage() {
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const [passwordValidation, setPasswordValidation] = useState<PasswordValidationResult>({
    isValid: false,
    errors: []
  });

  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const token = searchParams.get('token');
  const email = searchParams.get('email');

  useEffect(() => {
    if (password) setPasswordValidation(validatePassword(password));
  }, [password]);

  useEffect(() => {
    if (!token || !email) navigate('/forgot-password', { replace: true });
  }, [token, email, navigate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    const result = validatePassword(password);
    if (!result.isValid) {
      setError(new Error(result.errors.join(', ')));
      return;
    }

    if (password !== confirmPassword) {
      setError(new Error('Passwords do not match'));
      return;
    }

    if (!token || !email) {
      setError(new Error('Invalid reset link'));
      return;
    }

    setIsLoading(true);

    try {
      await authApi.resetPassword({ email, token, password });

      setIsSubmitted(true);
    } catch {
      setError(new Error('Failed to reset password. The reset link may be invalid or expired.'));
    } finally {
      setIsLoading(false);
    }
  };

  if (isSubmitted) {
    return (
      <AuthShell
        heading="Password Updated!"
        subheading="Your password has been updated. You can now sign in with your new password."
        topPrompt={
          <>
            Ready to continue?
            <Link to="/login" className="font-semibold text-accent hover:opacity-80 transition-opacity">
              Sign in
            </Link>
          </>
        }
        brandTitle={
          <>
            Your account is ready for the <span className="cairnly-text-gradient">next step</span>.
          </>
        }
        brandSubtitle="Return to Cairnly with a fresh password and keep your financial journey moving."
      >
        <div className="space-y-6 text-center">
          <Chip color="success" variant="soft" className="mx-auto">
            <MailCheck className="size-4" aria-hidden="true" />
            <span>Password Reset</span>
          </Chip>
          <Button fullWidth onPress={() => navigate('/login')}>
            Sign In
          </Button>
        </div>
      </AuthShell>
    );
  }

  if (!token || !email) {
    return (
      <AuthShell
        heading="Link Invalid or Expired"
        subheading="Please request a new password reset link."
        topPrompt={
          <>
            Remember your password?
            <Link to="/login" className="font-semibold text-accent hover:opacity-80 transition-opacity">
              Back to Sign In
            </Link>
          </>
        }
        brandTitle={
          <>
            Reset links stay short-lived to <span className="cairnly-text-gradient">protect your account</span>.
          </>
        }
        brandSubtitle="Request a new link whenever a password reset email expires or looks unexpected."
      >
        <div className="space-y-6 text-center">
          <Chip color="danger" variant="soft" className="mx-auto">
            <AlertCircle className="size-4" aria-hidden="true" />
            <span>Invalid Link</span>
          </Chip>
          <Button fullWidth onPress={() => navigate('/forgot-password')}>
            Request New Reset Link
          </Button>
        </div>
      </AuthShell>
    );
  }

  return (
    <AuthShell
      heading="Reset Password"
      subheading="Enter your new password below."
      topPrompt={
        <>
          Remember your password?
          <Link to="/login" className="font-semibold text-accent hover:opacity-80 transition-opacity">
            Back to Sign In
          </Link>
        </>
      }
      brandTitle={
        <>
          Re-secure your account and return to <span className="cairnly-text-gradient">clear money habits</span>.
        </>
      }
      brandSubtitle="Choose a strong password before you continue managing budgets, accounts, and spending."
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && <ApiErrorDisplay error={error} title="Password Reset Failed" showDetails={showErrorDetails} />}

        <div className="mb-4 rounded-lg border border-border bg-surface p-3">
          <p className="text-sm text-muted">
            <strong className="text-foreground">Resetting password for:</strong> {email}
          </p>
        </div>

        <FormField
          label="New Password"
          name="password"
          type="password"
          value={password}
          onChange={setPassword}
          isRequired
          isDisabled={isLoading}
          placeholder="Enter your new password"
          autoComplete="new-password"
          description={getPasswordRequirementsDescription()}
          isInvalid={password.length > 0 && !passwordValidation.isValid}
          errorMessage={
            password.length > 0 && !passwordValidation.isValid ? passwordValidation.errors.join(', ') : undefined
          }
        />

        <FormField
          label="Confirm New Password"
          name="confirmPassword"
          type="password"
          value={confirmPassword}
          onChange={setConfirmPassword}
          isRequired
          isDisabled={isLoading}
          placeholder="Confirm your new password"
          autoComplete="new-password"
          isInvalid={confirmPassword.length > 0 && password !== confirmPassword}
          errorMessage={
            confirmPassword.length > 0 && password !== confirmPassword ? 'Passwords do not match' : undefined
          }
        />

        <Button
          type="submit"
          fullWidth
          className="font-semibold mt-6"
          isPending={isLoading}
          isDisabled={!password || !confirmPassword || !passwordValidation.isValid || password !== confirmPassword}
        >
          {({ isPending }) => (
            <>
              {isPending && <Spinner color="current" size="sm" className="mr-2" />}
              {isPending ? 'Updating Password...' : 'Update Password'}
            </>
          )}
        </Button>
      </form>
    </AuthShell>
  );
}
