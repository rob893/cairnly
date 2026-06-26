import { useEffect, useState, useRef } from 'react';
import { useNavigate } from 'react-router';
import { Card, CardContent, CardHeader, Button, Spinner, Chip } from '@heroui/react';
import { CircleCheck, CircleX } from 'lucide-react';
import { useAuth } from '../hooks/useAuth';
import { GitHubIcon, GoogleIcon } from '../components/oauthIcons';
import { handleOAuthCallbackFromUrl, type OAuthProvider } from '../utils/oauthUtils';

/** Chip/Spinner accent color for a given step. */
type StepColor = 'accent' | 'success' | 'danger';

interface ProviderConfig {
  name: string;
  icon: React.JSX.Element;
}

const providerConfigs: Record<OAuthProvider, ProviderConfig> = {
  github: {
    name: 'GitHub',
    icon: <GitHubIcon className="w-8 h-8 text-foreground" />
  },
  google: {
    name: 'Google',
    icon: <GoogleIcon className="w-8 h-8" />
  }
};

export function OAuthCallbackPage({ provider }: { provider: OAuthProvider }) {
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [step, setStep] = useState<'processing' | 'exchanging' | 'authenticating' | 'success' | 'error'>('processing');
  const { loginWithGitHub, loginWithGoogle } = useAuth();
  const navigate = useNavigate();
  const hasProcessed = useRef(false);

  const providerConfig = providerConfigs[provider];

  useEffect(() => {
    if (hasProcessed.current) return;
    hasProcessed.current = true;

    const processCallback = async () => {
      try {
        setStep('processing');

        const result = handleOAuthCallbackFromUrl();

        // Scrub the single-use OAuth `code`/`state` from the URL and history once they have been
        // read and CSRF-verified, so they no longer linger in the address bar or window.history.
        // HashRouter keeps the route in the hash, so preserve the route portion and drop the query.
        const cleanHash = window.location.hash.split('?')[0];
        window.history.replaceState(null, '', window.location.pathname + window.location.search + cleanHash);

        if (!result) {
          setError('Invalid callback parameters');
          setStep('error');
          return;
        }

        if (result.error) {
          setError(result.errorDescription || result.error);
          setStep('error');
          return;
        }

        if (!result.code) {
          setError('No authorization code received');
          setStep('error');
          return;
        }
        setStep('exchanging');
        setStep('authenticating');

        if (provider === 'github') {
          await loginWithGitHub(result.code);
        } else {
          await loginWithGoogle(result.code);
        }

        setStep('success');
        await new Promise(resolve => setTimeout(resolve, 800)); // Show success state
        navigate('/dashboard', { replace: true });
      } catch (err) {
        setError(err instanceof Error ? err.message : `${providerConfig?.name || 'OAuth'} login failed`);
        setStep('error');
      } finally {
        setIsLoading(false);
      }
    };

    processCallback();
  }, [loginWithGitHub, loginWithGoogle, navigate, provider, providerConfig?.name]);

  const getStepInfo = (): { title: string; description: string; icon: React.JSX.Element; color: StepColor } => {
    const providerName = providerConfig?.name || 'OAuth';

    switch (step) {
      case 'processing':
        return {
          title: 'Processing Callback',
          description: `Validating ${providerName} authorization...`,
          icon: <Spinner size="lg" color="accent" />,
          color: 'accent'
        };
      case 'exchanging':
        return {
          title: 'Exchanging Tokens',
          description: 'Securely exchanging authorization code...',
          icon: <Spinner size="lg" color="accent" />,
          color: 'accent'
        };
      case 'authenticating':
        return {
          title: 'Authenticating',
          description: 'Completing your login to Cairnly...',
          icon: <Spinner size="lg" color="accent" />,
          color: 'accent'
        };
      case 'success':
        return {
          title: 'Login Successful!',
          description: 'Welcome to Cairnly! Redirecting you now...',
          icon: <CircleCheck className="w-16 h-16 text-success" />,
          color: 'success'
        };
      case 'error':
        return {
          title: 'Login Failed',
          description: `Something went wrong during the ${providerName} login process.`,
          icon: <CircleX className="w-16 h-16 text-danger" />,
          color: 'danger'
        };
      default:
        return {
          title: 'Processing...',
          description: 'Please wait...',
          icon: <Spinner size="lg" color="accent" />,
          color: 'accent'
        };
    }
  };

  const stepInfo = getStepInfo();

  if (!providerConfig) {
    return (
      <div className="min-h-screen bg-linear-to-br from-background via-surface to-background flex items-center justify-center p-4">
        <Card className="w-full max-w-md shadow-2xl border border-border">
          <CardContent className="px-8 py-8 text-center">
            <CircleX className="w-16 h-16 text-danger mx-auto mb-4" />
            <h2 className="text-2xl font-bold text-foreground mb-2">Invalid Provider</h2>
            <p className="text-muted mb-6">Unsupported OAuth provider: {provider}</p>
            <Button fullWidth size="lg" className="font-semibold" onPress={() => navigate('/login', { replace: true })}>
              Return to Login
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-linear-to-br from-background via-surface to-background flex items-center justify-center p-4">
      <Card className="w-full max-w-md shadow-2xl border border-border">
        <CardHeader className="flex flex-col items-center pb-6 pt-8">
          {/* Provider icon */}
          <div className="mb-4 p-3 rounded-full bg-surface-secondary border border-border">{providerConfig.icon}</div>

          <Chip color={stepInfo.color} variant="soft" className="font-medium mb-2">
            {providerConfig.name} Authentication
          </Chip>
        </CardHeader>

        <CardContent className="px-8 pb-8 text-center">
          {/* Status icon */}
          <div className="flex justify-center mb-6">{stepInfo.icon}</div>

          {/* Status text */}
          <div className="space-y-3 mb-6">
            <h2 className="text-2xl font-bold text-foreground">{stepInfo.title}</h2>
            <p className="text-muted text-lg">{stepInfo.description}</p>
          </div>

          {/* Progress steps */}
          {isLoading && step !== 'error' && (
            <div className="flex justify-center items-center space-x-2 mb-6">
              <div
                className={`w-2 h-2 rounded-full ${['processing', 'exchanging', 'authenticating', 'success'].includes(step) ? 'bg-accent' : 'bg-surface-secondary'}`}
              />
              <div
                className={`w-2 h-2 rounded-full ${['exchanging', 'authenticating', 'success'].includes(step) ? 'bg-accent' : 'bg-surface-secondary'}`}
              />
              <div
                className={`w-2 h-2 rounded-full ${['authenticating', 'success'].includes(step) ? 'bg-accent' : 'bg-surface-secondary'}`}
              />
              <div className={`w-2 h-2 rounded-full ${step === 'success' ? 'bg-success' : 'bg-surface-secondary'}`} />
            </div>
          )}

          {/* Error details */}
          {error && step === 'error' && (
            <div className="mb-6 p-4 bg-danger/10 border border-danger/20 rounded-lg">
              <p className="text-danger text-sm font-medium">{error}</p>
            </div>
          )}

          {/* Action button for error state */}
          {step === 'error' && (
            <Button fullWidth size="lg" className="font-semibold" onPress={() => navigate('/login', { replace: true })}>
              Return to Login
            </Button>
          )}

          {/* Loading state message */}
          {isLoading && step !== 'error' && <p className="text-sm text-muted">This may take a few moments...</p>}
        </CardContent>
      </Card>
    </div>
  );
}
