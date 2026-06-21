import React from 'react';
import { AppHeader } from '../components/AppHeader';

interface AppLayoutProps {
  children: React.ReactNode;
}

export function AppLayout({ children }: AppLayoutProps) {
  return (
    <div className="min-h-screen bg-background flex flex-col">
      <AppHeader />
      <main className="flex-1 py-8">
        <div className="w-full max-w-[2000px] mx-auto px-6 lg:px-10">{children}</div>
      </main>
    </div>
  );
}
