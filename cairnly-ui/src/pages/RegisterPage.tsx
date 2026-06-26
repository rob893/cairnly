import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router';
import { Button, Separator, Spinner } from '@heroui/react';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { showErrorDetails } from '../utils/environment';
import { FormField } from '../components/FormField';
import { SocialLoginButtons } from '../components/SocialLoginButtons';
import { AuthShell } from '../components/AuthShell';
import { useAuth } from '../hooks/useAuth';
import {
  validatePassword,
  getPasswordRequirementsDescription,
  type PasswordValidationResult
} from '../utils/passwordValidation';

export function RegisterPage() {
  const [userName, setUserName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [passwordValidation, setPasswordValidation] = useState<PasswordValidationResult>({
    isValid: false,
    errors: []
  });

  const { register } = useAuth();
  const navigate = useNavigate();

  const handlePasswordChange = (value: string) => {
    setPassword(value);
    setPasswordValidation(validatePassword(value));
  };

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

    setIsLoading(true);

    try {
      await register({ userName, email, password });
      navigate('/dashboard', { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err : new Error('Registration failed'));
    } finally {
      setIsLoading(false);
    }
  };

  const isDisabled =
    !userName || !email || !password || !confirmPassword || !passwordValidation.isValid || password !== confirmPassword;

  return (
    <AuthShell
      heading="Create your account"
      subheading="Start tracking spending plans, accounts, and spending in minutes."
      topPrompt={
        <>
          Already have an account?
          <Link to="/login" className="font-semibold text-accent hover:opacity-80 transition-opacity">
            Sign in
          </Link>
        </>
      }
      brandTitle={
        <>
          Treat your money like an <span className="cairnly-text-gradient">observable system</span>.
        </>
      }
      brandSubtitle="Small, consistent actions stack into real progress. Cairnly gives you one clear picture of it all."
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && <ApiErrorDisplay error={error} title="Registration Failed" showDetails={showErrorDetails} />}

        <FormField
          label="Username"
          name="userName"
          value={userName}
          onChange={setUserName}
          isRequired
          isDisabled={isLoading}
          placeholder="Choose a username"
          autoComplete="username"
        />

        <FormField
          label="Email"
          name="email"
          type="email"
          value={email}
          onChange={setEmail}
          isRequired
          isDisabled={isLoading}
          placeholder="Enter your email"
          autoComplete="email"
        />

        <FormField
          label="Password"
          name="password"
          type="password"
          value={password}
          onChange={handlePasswordChange}
          isRequired
          isDisabled={isLoading}
          placeholder="Choose a password"
          autoComplete="new-password"
          description={getPasswordRequirementsDescription()}
          isInvalid={password.length > 0 && !passwordValidation.isValid}
          errorMessage={
            password.length > 0 && !passwordValidation.isValid ? passwordValidation.errors.join(', ') : undefined
          }
        />

        <FormField
          label="Confirm Password"
          name="confirmPassword"
          type="password"
          value={confirmPassword}
          onChange={setConfirmPassword}
          isRequired
          isDisabled={isLoading}
          placeholder="Confirm your password"
          autoComplete="new-password"
          isInvalid={confirmPassword.length > 0 && password !== confirmPassword}
          errorMessage={
            confirmPassword.length > 0 && password !== confirmPassword ? 'Passwords do not match' : undefined
          }
        />

        <Button type="submit" fullWidth className="font-semibold" isPending={isLoading} isDisabled={isDisabled}>
          {({ isPending }) => (
            <>
              {isPending && <Spinner color="current" size="sm" className="mr-2" />}
              {isPending ? 'Creating Account...' : 'Create Account'}
            </>
          )}
        </Button>
      </form>

      <Separator className="my-6" />

      <SocialLoginButtons isDisabled={isLoading} onError={setError} />
    </AuthShell>
  );
}
