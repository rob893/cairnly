import { readFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';
import { describe, expect, it } from 'vitest';

const testDirectory = dirname(fileURLToPath(import.meta.url));
const homePageSource = readFileSync(resolve(testDirectory, '..', 'HomePage.tsx'), 'utf8');

describe('HomePage chart loading', () => {
  it('lazy-loads the Recharts dashboard card instead of importing it statically', () => {
    expect(homePageSource).not.toMatch(/import\s+\{\s*SpendingChartCard/);
    expect(homePageSource).toMatch(/lazy\(\(\)\s*=>\s*import\('\.\.\/components\/dashboard\/SpendingChartCard'\)/);
    expect(homePageSource).toContain('<Suspense');
  });
});
