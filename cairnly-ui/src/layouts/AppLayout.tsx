import { useState } from 'react';
import type { ReactNode } from 'react';
import { AppHeader } from '../components/AppHeader';
import { AppSidebar } from '../components/AppSidebar';
import { PageHeaderProvider } from '../contexts/PageHeaderContext';

interface AppLayoutProps {
  children: ReactNode;
}

/**
 * The authenticated application shell: a fixed left sidebar (collapsible to a
 * drawer on small screens), a slim top bar, and the scrollable page content.
 * Pages set the top-bar title and actions via {@link usePageHeader}.
 */
export function AppLayout({ children }: AppLayoutProps) {
  const [sidebarOpen, setSidebarOpen] = useState(false);

  return (
    <PageHeaderProvider>
      <div className="min-h-screen bg-background">
        <AppSidebar isOpen={sidebarOpen} onClose={() => setSidebarOpen(false)} />

        <div className="flex min-h-screen flex-col lg:pl-64">
          <AppHeader onMenuClick={() => setSidebarOpen(true)} />
          <main className="flex-1 px-4 py-6 sm:px-6 lg:px-8">{children}</main>
        </div>
      </div>
    </PageHeaderProvider>
  );
}
