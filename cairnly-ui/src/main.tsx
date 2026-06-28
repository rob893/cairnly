import React from 'react';
import ReactDOM from 'react-dom/client';
import { HashRouter as Router } from 'react-router';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import App from './App.tsx';
import { ErrorBoundary } from './components/ErrorBoundary';
import { LazyDevTools } from './components/LazyDevTools';
import { ThemeProvider } from './contexts/ThemeContext';
import './index.css';

// HeroUI v3 — styles imported via @import "@heroui/styles" in index.css, no provider needed.

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (failureCount, error) => {
        if (error && typeof error === 'object' && 'status' in error && error.status === 404) {
          return false;
        }
        return failureCount < 2;
      },
      refetchOnWindowFocus: false,
      staleTime: 60 * 1000
    },
    mutations: {
      retry: false
    }
  }
});

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ThemeProvider>
      <QueryClientProvider client={queryClient}>
        <Router>
          <ErrorBoundary>
            <App />
          </ErrorBoundary>
          <LazyDevTools />
        </Router>
      </QueryClientProvider>
    </ThemeProvider>
  </React.StrictMode>
);
