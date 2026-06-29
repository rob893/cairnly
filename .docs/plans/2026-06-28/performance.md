# Performance Research — 2026-06-28

## Executive Summary

The backend is in strong shape: no blocking `.Result`/`.Wait()` calls, `CancellationToken` is propagated throughout, balances are snapshot-backed with `SUM`/`GROUP BY` aggregation (not row-by-row replay), and the transactions table already has an `IX_Transactions_AccountId` index that lets the balance-sum queries seek rather than scan. The impactful opportunities are all on the frontend, centered on the transactions list, which eagerly drains every page over sequential round-trips and renders every row, and on shaving the post-login critical path. Four findings pass the gate; the bundle is otherwise well chunked and routes/devtools are lazy-loaded.

## Status

| #   | Finding                                                       | Impact | Effort | Status       |
| --- | ------------------------------------------------------------ | ------ | ------ | ------------ |
| 1   | TransactionsTable drains all pages; no virtualization        | High   | Medium | ✅ Done (W2) |
| 2   | Charts vendor chunk on dashboard critical path               | Medium | Low    | ✅ Done (W1) |
| 3   | Dashboard fans out one summary request per spending plan     | Medium | Medium | ✅ Done (W2) |
| 4   | Whole HeroUI shipped via barrel import                        | Medium | Medium | ✅ Done (W2) |

## Findings

### 1. TransactionsTable drains all pages sequentially and renders every row (no virtualization)

- **Description:** `TransactionsTable` auto-fetches every page via an effect (`if (query.hasNextPage && !query.isFetchingNextPage) fetchNextPage()`), then `flatMap`s all pages and renders one `<TransactionRow>` per transaction grouped by day. With `PAGE_SIZE = 100`, a user with ~5,000 transactions triggers ~50 *sequential* HTTP round-trips (each page only starts after the previous resolves) and materializes ~5,000 inline-editable DOM rows. Rows are `memo`'d, but DOM count and the serial fetch waterfall dominate. This is the highest-volume entity and the page is rendered both globally and per-account.
- **Location:** `cairnly-ui/src/components/transactions/TransactionsTable.tsx:12` (`PAGE_SIZE = 100`), `:123-128` (eager `fetchNextPage` loop), `:136-139` (`flatMap` all pages), `:240-266` (renders every group/row); hooks at `cairnly-ui/src/hooks/transactions.ts:31-39,51-59`.
- **Impact:** High
- **Effort:** Medium (1-4hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Stop draining all pages. Render visible pages and add an IntersectionObserver "load more" sentinel (or a button) so paging is on-demand, and virtualize rows for large sets (`@tanstack/react-virtual`). If a complete client-side grouped view is required, raise `PAGE_SIZE` (e.g. 250) to cut round-trips and cap total rows with a "show all" affordance. Even just on-demand paging removes the serial waterfall.
- **What was done (W2):** Removed the eager `fetchNextPage` drain in `TransactionsTable` so pages load on demand via a sentinel, and added `@tanstack/react-virtual` row virtualization so only visible rows render — eliminating the ~50-request serial waterfall and ~5,000-row DOM. Inline edit + day grouping preserved.

### 2. Charts vendor chunk (~121 KB gzip) is on the dashboard's critical path

- **Description:** `vendor-charts` (recharts + d3) is 431 KB raw / **121 KB gzip** and `vendor-ui` (HeroUI) is 523 KB raw / 159 KB gzip. The post-login landing route is `HomePage`, which statically imports `SpendingChartCard` → recharts, so the charts chunk is pulled into the dashboard's initial paint even though the chart sits below the stat cards. Vendor splitting is otherwise correctly configured in `vite.config.ts`.
- **Location:** `cairnly-ui/src/pages/HomePage.tsx:7` (`import { SpendingChartCard }`), `cairnly-ui/src/components/dashboard/SpendingChartCard.tsx:1` (recharts import); chunking config `cairnly-ui/vite.config.ts:55-67`.
- **Impact:** Medium
- **Effort:** Low (< 1hr)
- **Recommendation:** `React.lazy` the chart cards (`SpendingChartCard`, `CashFlowChart`, `NetWorthCard`) behind `Suspense` so the recharts chunk loads after first paint of the stat cards. Defers ~121 KB gzip off the initial dashboard render with no UX loss.
- **What was done (W1):** `HomePage` now `React.lazy`-loads the chart cards behind `Suspense`, so the ~121 KB gzip recharts chunk loads after the stat cards paint instead of blocking initial dashboard render.
- **Dependencies:** None
- **Breaking Changes:** No

### 3. Dashboard fans out one summary request per spending plan

- **Description:** `HomePage` lists all plans then calls `useSpendingPlanSummaries`, which issues one `GET /spending-plans/{id}/summary` per plan via `useQueries`. Each request re-loads that plan's incomes+expenses server-side (`SpendingPlanService.GetSpendingPlanSummaryAsync` runs two `SearchAsync` calls). Parallelized, so not a waterfall, but N plans = N requests + 2N queries every dashboard load. Tolerable at a handful of plans; degrades as plans grow.
- **Location:** `cairnly-ui/src/pages/HomePage.tsx:25-26`, `cairnly-ui/src/hooks/spendingPlans.ts:87-109`; backend `Cairnly.API/Services/Domain/SpendingPlanService.cs:104-105`.
- **Impact:** Medium
- **Effort:** Medium (1-4hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Add a batch summaries endpoint (e.g. `GET /spending-plans/summaries`) that aggregates all plans for the user in one query, and back it with a single `useQuery`. Keep `staleTime` so the dashboard isn't refetching per navigation.
- **What was done (W2):** Added `GET /api/v1/spending-plans/summaries` (`SpendingPlanService.GetSpendingPlanSummariesAsync`) returning all the user's plan summaries, and switched `HomePage` from N `useQueries` to one `useQuery` — N requests + 2N queries collapse to a single round-trip.

### 4. Whole HeroUI library shipped via barrel import (~159 KB gzip, all routes)

- **Description:** Every component imports from the `@heroui/react` barrel, so `vendor-ui` is 523 KB raw / 159 KB gzip and downloads on the public login/landing routes that use only a few primitives. The barrel limits tree-shaking. Splitting already isolates it as a cacheable chunk, so this is initial-load weight, not per-deploy.
- **Location:** barrel imports across `cairnly-ui/src/**` (e.g. `pages/AccountsPage.tsx:2`, `components/transactions/TransactionsTable.tsx:2`); chunk defined `cairnly-ui/vite.config.ts:59`.
- **Impact:** Medium
- **Effort:** Medium (1-4hr) — verify HeroUI v3 deep-import support first
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Confirm whether `@heroui/react` is side-effect-free / supports per-component entry points; if so, switch hot paths to deep imports. Otherwise leave as-is — gzip 159 KB cached cross-deploy is acceptable, so only pursue if measured TTI is a concern.
- **P4 verification:** HeroUI v3.2.1 supports side-effect-free deep imports, but converting imports only reduced `vendor-ui` from 166.50 KB gzip to 165.95 KB gzip, so source was left as-is.
- **What was done (W2):** Verified `@heroui/react` v3.2.1 exposes `sideEffects:false` + per-component `exports`; a deep-import trial saved ~0.5 KB gzip (166.50→165.95), below the noise floor, so no source changes were kept — closed as an intentional no-op.

## Notes (verified clean — no action)

- Backend: no blocking calls, `CancellationToken` propagated, balances snapshot-backed with `SUM`/`GROUP BY`, `IX_Transactions_AccountId` present, cursor pagination everywhere.
- Frontend: routes + devtools lazy-loaded, React Query `staleTime` tuned (60s default / 30s lists / 5m categories), `useInfiniteQuery`/`useQueries` used correctly, contexts memoized, table rows `memo`'d.
