import { test, expect } from '@playwright/test';

test('app loads and shows the landing page', async ({ page }) => {
  await page.goto('/');
  await page.waitForURL(url => url.hash.includes('/') || url.hash === '' || url.pathname === '/', { timeout: 5000 });

  // The public landing page renders its hero heading for unauthenticated visitors.
  const heading = page.getByRole('heading', { name: /financial clarity/i });
  await expect(heading).toBeVisible({ timeout: 5000 });
});
