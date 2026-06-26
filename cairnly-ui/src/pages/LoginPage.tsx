import React, { useState } from 'react';
import { Link, useNavigate, useLocation } from 'react-router';
import { Button, Separator, Spinner } from '@heroui/react';
import { ApiErrorDisplay } from '../components/ApiErrorDisplay';
import { showErrorDetails } from '../utils/environment';
import { FormField } from '../components/FormField';
import { SocialLoginButtons } from '../components/SocialLoginButtons';
import { AuthShell } from '../components/AuthShell';
import { useAuth } from '../hooks/useAuth';

export function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<Error | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = location.state?.from?.pathname || '/dashboard';

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      await login({ userName: username, password });
      navigate(from, { replace: true });
    } catch (err) {
      setError(err instanceof Error ? err : new Error('Login failed'));
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <AuthShell
      heading="Sign in to your account"
      subheading="Welcome back. Pick up right where you left off."
      topPrompt={
        <>
          New to Cairnly?
          <Link to="/register" className="font-semibold text-accent hover:opacity-80 transition-opacity">
            Create an account
          </Link>
        </>
      }
      brandTitle={
        <>
          Financial clarity for the <span className="cairnly-text-gradient">long journey</span>.
        </>
      }
      brandSubtitle="SpendingPlans, accounts, and spending in one calm, clear place. Build wealth one marker at a time."
    >
      <form onSubmit={handleSubmit} className="space-y-5">
        {error && <ApiErrorDisplay error={error} title="Login Failed" showDetails={showErrorDetails} />}

        <FormField
          label="Username or Email"
          value={username}
          onChange={setUsername}
          isRequired
          isDisabled={isLoading}
          placeholder="Enter your username or email"
          autoComplete="username"
        />

        <FormField
          label="Password"
          type="password"
          value={password}
          onChange={setPassword}
          isRequired
          isDisabled={isLoading}
          placeholder="Enter your password"
          autoComplete="current-password"
        />

        <div className="flex justify-end">
          <Link to="/forgot-password" className="text-sm text-accent hover:opacity-80 transition-opacity">
            Forgot your password?
          </Link>
        </div>

        <Button
          type="submit"
          fullWidth
          className="font-semibold"
          isPending={isLoading}
          isDisabled={!username || !password}
        >
          {({ isPending }) => (
            <>
              {isPending && <Spinner color="current" size="sm" className="mr-2" />}
              {isPending ? 'Signing In...' : 'Sign In'}
            </>
          )}
        </Button>
      </form>

      <Separator className="my-6" />

      <SocialLoginButtons isDisabled={isLoading} onError={setError} />
    </AuthShell>
  );
}
