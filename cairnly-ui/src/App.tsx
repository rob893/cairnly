import { lazy, Suspense } from 'react';
import { Routes, Route, Navigate } from 'react-router';
import { Spinner } from '@heroui/react';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { ThemeSync } from './components/ThemeSync';
import { AppLayout } from './layouts/AppLayout';
import { routePaths } from './constants/routes';

const LandingPage = lazy(() => import('./pages/LandingPage').then(m => ({ default: m.LandingPage })));
const HomePage = lazy(() => import('./pages/HomePage').then(m => ({ default: m.HomePage })));
const AccountsPage = lazy(() => import('./pages/AccountsPage').then(m => ({ default: m.AccountsPage })));
const AccountDetailPage = lazy(() => import('./pages/AccountDetailPage').then(m => ({ default: m.AccountDetailPage })));
const TransactionsPage = lazy(() => import('./pages/TransactionsPage').then(m => ({ default: m.TransactionsPage })));
const CashFlowPage = lazy(() => import('./pages/CashFlowPage').then(m => ({ default: m.CashFlowPage })));
const SpendingPlansPage = lazy(() => import('./pages/SpendingPlansPage').then(m => ({ default: m.SpendingPlansPage })));
const SpendingPlanDetailPage = lazy(() =>
  import('./pages/SpendingPlanDetailPage').then(m => ({ default: m.SpendingPlanDetailPage }))
);
const SettingsPage = lazy(() => import('./pages/SettingsPage').then(m => ({ default: m.SettingsPage })));
const LoginPage = lazy(() => import('./pages/LoginPage').then(m => ({ default: m.LoginPage })));
const RegisterPage = lazy(() => import('./pages/RegisterPage').then(m => ({ default: m.RegisterPage })));
const OAuthCallbackPage = lazy(() => import('./pages/OAuthCallbackPage').then(m => ({ default: m.OAuthCallbackPage })));
const ForgotPasswordPage = lazy(() =>
  import('./pages/ForgotPasswordPage').then(m => ({ default: m.ForgotPasswordPage }))
);
const ResetPasswordPage = lazy(() => import('./pages/ResetPasswordPage').then(m => ({ default: m.ResetPasswordPage })));

function PageFallback() {
  return (
    <div className="flex min-h-screen items-center justify-center">
      <Spinner size="lg" color="accent" />
    </div>
  );
}

function App() {
  return (
    <AuthProvider>
      <ThemeSync />
      <div className="app min-h-screen bg-background text-foreground">
        <Suspense fallback={<PageFallback />}>
          <Routes>
            {/* Public landing */}
            <Route path={routePaths.landing} element={<LandingPage />} />

            {/* Auth routes (unauthenticated) */}
            <Route path={routePaths.login} element={<LoginPage />} />
            <Route path={routePaths.register} element={<RegisterPage />} />
            <Route path={routePaths.forgotPassword} element={<ForgotPasswordPage />} />
            <Route path={routePaths.resetPassword} element={<ResetPasswordPage />} />
            <Route path={routePaths.gitHubCallback} element={<OAuthCallbackPage provider="github" />} />
            <Route path={routePaths.googleCallback} element={<OAuthCallbackPage provider="google" />} />

            {/* Protected routes */}
            <Route
              path={routePaths.dashboard}
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <HomePage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path={routePaths.accounts}
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <AccountsPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path={routePaths.accountDetail}
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <AccountDetailPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path={routePaths.transactions}
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <TransactionsPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path={routePaths.cashFlow}
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <CashFlowPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path={routePaths.spendingPlans}
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <SpendingPlansPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path={routePaths.spendingPlanDetail}
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <SpendingPlanDetailPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />
            <Route
              path={routePaths.settings}
              element={
                <ProtectedRoute>
                  <AppLayout>
                    <SettingsPage />
                  </AppLayout>
                </ProtectedRoute>
              }
            />

            {/* Catch-all */}
            <Route path="*" element={<Navigate to={routePaths.landing} replace />} />
          </Routes>
        </Suspense>
      </div>
    </AuthProvider>
  );
}

export default App;
