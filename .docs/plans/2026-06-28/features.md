# Feature Research — 2026-06-28

## Executive Summary

Cairnly's foundation (accounts, transactions, categories, tags, net-worth history, spending plans) is solid, but the highest-value spend "clarity" surfaces are still stubs: the Cash Flow page renders entirely from `mockCashFlow.ts`, the dashboard charts only *planned* spending-plan numbers, and there is no actual-vs-plan, goals, recurring detection, or import. The biggest wins are turning existing screens into real, transaction-backed insights and adding the Monarch-style "Budget" feature that the codebase reserves. The top five below all build directly on existing entities/services and map onto the team's own roadmap in `.docs/specs`.

## Status

| #   | Feature                                              | Impact   | Effort    | Status     |
| --- | --------------------------------------------------- | -------- | --------- | ---------- |
| 1   | Real Cash Flow & spending-trends API                | High     | High      | ⬜ Pending |
| 2   | Budgets (per-category limits + budget-vs-actual)    | Critical | Very High | ⬜ Pending |
| 3   | Goals with progress + projected completion          | High     | High      | ⬜ Pending |
| 4   | Recurring / subscription detection                  | High     | Very High | ⬜ Pending |
| 5   | CSV transaction import                               | High     | High      | ⬜ Pending |

## Findings

### 1. Real Cash Flow & spending-trends API (retire `mockCashFlow.ts`)
- **Description:** `CashFlowPage` is fully placeholder — income/expense series, summary, and category breakdowns all come from `mockCashFlow.ts`. Every input needed (signed `Transaction.Amount`, `Date`, `CategoryId`, `Category.Kind`) already exists, so a single read-aggregate endpoint converts a fake screen into a trusted one: monthly income/expense/net, savings rate, and top spend categories. This is the cheapest path to real "clarity" and directly powers the dashboard (Pillar 4) and Insights (Pillar 9).
- **Location:** `cairnly-ui/src/pages/CashFlowPage.tsx`, `cairnly-ui/src/constants/mockCashFlow.ts`, `cairnly-ui/src/components/cashflow/CashFlowChart.tsx`, `CategoryBreakdown.tsx`; API: model on `AccountRepository.GetTransactionSumsAsync` group-by pattern (`Cairnly.API/Data/Repositories/AccountRepository.cs`), `TransactionRepository`, `CategoryKind` (Income/Expense/Transfer), exclude `IsBalanceAdjustment`/split children.
- **Impact:** High
- **Effort:** High
- **Dependencies:** None (transactions exist today); pairs with finding #2 for "budget utilization."
- **Breaking Changes:** No
- **Recommendation:** Add `GET /api/v1/reports/cashflow?period=monthly&from=…&to=…` returning per-period income/expense/net + per-category rollups, grouped via EF `GroupBy` on `date_trunc` + `CategoryId`, filtering out `ParentTransactionId != null` and `IsBalanceAdjustment`. Expose via `hooks/api.ts` query, delete the mock import. Reuse currency from accounts.

### 2. Budgets (per-category limits + budget-vs-actual)
- **Description:** The "Budget" name is reserved for a Monarch-style tracker; today only forward-looking SpendingPlans exist (no comparison to real spend). A `Budget` entity (category + period + amount + optional rollover) plus a status aggregate answers "Am I on track? / $350 left this month" — the core of Pillar 1 and the dashboard's "budget utilization" metric. SpendingPlan + cadence give a near-copy template, and categorized transactions provide actuals.
- **Location:** mirror `SpendingPlan*` stack — `Cairnly.API/Models/Entities/SpendingPlan.cs`, `Services/Domain/SpendingPlanService.cs`, `Controllers/V1/SpendingPlansController.cs`, `SpendingPlanCadence.cs`; actuals from `Transaction`/`Category`; UI alongside `pages/SpendingPlansPage.tsx` + `components/spendingPlans/SpendingPlanSummaryCard.tsx`.
- **Impact:** Critical
- **Effort:** Very High
- **Dependencies:** Category aggregation from #1 (shared spend rollup); ties to envelope rollover open question in `.docs/specs/01-spending-and-budgeting.md`.
- **Breaking Changes:** No (additive entity + migration)
- **Recommendation:** New `Budget` entity (`IOwnedByUser`/`IAuditableEntity`), `BudgetService` returning `GET /budgets/status?period=monthly` (planned vs summed actuals + remaining + % used). Add migration, repo, cursor list. Surface "budget vs actual" cards reusing the cadence-table UI.

### 3. Goals with progress + projected completion
- **Description:** Savings goals (emergency fund, down payment, payoff) give saving a purpose and are a proven retention/engagement driver — progress rings and "set aside $X/mo to finish by <date>." `linkedAccountId` can derive progress straight from the existing balance resolver, so an asset account's growth auto-fills the bar.
- **Location:** reuse balance math in `Services/Domain/AccountBalanceResolver.cs` + `BalanceHistoryService.cs` (for funding pace); pattern off `SpendingPlanService.cs`; new page beside `pages/AccountsPage.tsx`; spec `.docs/specs/05-goals.md`.
- **Impact:** High
- **Effort:** High
- **Dependencies:** None required; richer "funding capacity" projection later consumes #1/cash-flow surplus.
- **Breaking Changes:** No
- **Recommendation:** `Goal` entity (target, current, targetDate?, linkedAccountId?, status) + `GET/POST/PUT/DELETE /goals` and `GET /goals/{id}/projection` (completion from contribution rate, recommended monthly). Progress = manual contributions or linked-account balance via `IAccountBalanceResolver`. Goal cards w/ rings.

### 4. Recurring transaction / subscription detection
- **Description:** Surface the subscriptions and bills hiding in the stream ("$214/mo across 9 subscriptions; Netflix +17%"). Cluster transactions by normalized `Merchant` + regular interval + similar amount into a `RecurringSeries`; high curiosity/return value and the input forecasting needs. Data is present; `Transaction.Merchant` and date ordering already exist.
- **Location:** `Cairnly.API/Models/Entities/Transaction.cs` (`Merchant`, `Date`, `Amount`), `TransactionRepository` filters; new detection service; UI alongside `pages/TransactionsPage.tsx`; spec `.docs/specs/07-recurring-transaction-detection.md`.
- **Impact:** High
- **Effort:** Very High
- **Dependencies:** Cleaner with merchant normalization (shared with a future rules engine); best after #5 import boosts data volume but works on manual data.
- **Breaking Changes:** No
- **Recommendation:** `RecurringSeries` entity + heuristic clusterer (≥3 occurrences, cadence + amount tolerance). `GET /recurring`, `POST /recurring/{id}/confirm|ignore`, `/history` for price-drift. Persist `recurringSeriesId` on Transaction. Subscriptions list w/ monthly cost + sparkline.

### 5. CSV transaction import (staged → confirm)
- **Description:** Manual-only entry caps account size and retention; `TransactionSource.Import` already exists but no endpoint does. A column-mapping import wizard (upload → preview → commit) removes the biggest onboarding friction and unlocks recurring detection (#4) and a future rules engine with real data volume.
- **Location:** `Cairnly.API/Models/Entities/TransactionSource.cs` (Import already defined), `Services/Domain/TransactionService.CreateTransactionAsync` (reuse validation/snapshot path), `cairnly-ui/src/components/transactions/TransactionFormModal.tsx`; spec `.docs/specs/01-spending-and-budgeting.md`.
- **Impact:** High
- **Effort:** High
- **Dependencies:** Categories must exist (they do); feeds #4 recurring and the rules engine.
- **Breaking Changes:** No
- **Recommendation:** `POST /transactions/import` accepting `IFormFile` → parse to staged rows, generic column-map UI, then bulk-create with `Source=Import` and account/balance-snapshot reuse. De-dupe on date+amount+merchant. Server-side amount-as-minor-units parsing to match existing precision rules.
