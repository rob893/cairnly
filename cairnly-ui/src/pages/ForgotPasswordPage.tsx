import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { Button, Chip, Spinner } from '@heroui/react';
import { MailCheck } from 'lucide-react';
import { AuthShell } from '../components/AuthShell';
import { FormField } from '../components/FormField';
import { authApi } from '../services/auth';

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [isSubmitted, setIsSubmitted] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);

    try {
      await authApi.forgotPassword({ email });
    } catch {
      // Always show success per security best practices (avoid account enumeration)
    } finally {
      setIsLoading(false);
      setIsSubmitted(true);
    }
  };

  if (isSubmitted) {
    return (
      <AuthShell
        heading="Check Your Email"
        subheading="If an account with that email exists, we've sent a password reset link. Please check your inbox."
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
            Account security with <span className="cairnly-text-gradient">calm confirmation</span>.
          </>
        }
        brandSubtitle="Cairnly keeps password recovery clear without revealing whether an email belongs to an account."
      >
        <div className="space-y-6 text-center">
          <Chip color="success" variant="soft" className="mx-auto">
            <MailCheck className="size-4" aria-hidden="true" />
            <span>Email Sent</span>
          </Chip>
          <p className="text-sm text-muted">Didn't receive an email? Check your spam folder or try again.</p>

          <div className="space-y-3">
            <Button fullWidth onPress={() => navigate('/login')}>
              Back to Sign In
            </Button>
            <Button
              variant="ghost"
              fullWidth
              onPress={() => {
                setIsSubmitted(false);
                setEmail('');
              }}
            >
              Try Different Email
            </Button>
          </div>
        </div>
      </AuthShell>
    );
  }

  return (
    <AuthShell
      heading="Forgot Password"
      subheading="Enter your email and we'll send you a link to reset your password."
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
          A safer path back to your <span className="cairnly-text-gradient">financial plan</span>.
        </>
      }
      brandSubtitle="Request a reset link and return to tracking budgets, accounts, and spending with confidence."
    >
      <form onSubmit={handleSubmit} className="space-y-6">
        <FormField
          label="Email Address"
          type="email"
          value={email}
          onChange={setEmail}
          isRequired
          isDisabled={isLoading}
          placeholder="Enter your email address"
          autoComplete="email"
          description="We'll send a password reset link to this address"
        />

        <Button type="submit" fullWidth className="font-semibold" isPending={isLoading} isDisabled={!email}>
          {({ isPending }) => (
            <>
              {isPending && <Spinner color="current" size="sm" className="mr-2" />}
              {isPending ? 'Sending Reset Link...' : 'Send Reset Link'}
            </>
          )}
        </Button>
      </form>
    </AuthShell>
  );
}
