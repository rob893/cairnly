# Quality Research — 2026-06-28

## Executive Summary

The Cairnly codebase is well-disciplined overall: no files exceed 1000 lines, suppressions are
minimal (3 `react-hooks/exhaustive-deps` disables, zero `any`/`@ts-ignore`, pragmas only in generated
migrations), and exception "swallowing" is intentional and logged. The highest-leverage issues are a
fully duplicated income/expense vertical on the backend and a handful of oversized, multi-responsibility
files (one controller, three React components) that should be split. Reported below are the top 5
new findings.

## Metrics

- Files > 1000 lines: 0
- Files > 500 lines: 6 (BE: `CollectionExtensions.cs` 697, `AuthController.cs` 668, `EntityFrameworkExtensions.cs` 526; FE: `LineItemsSection.tsx` 760, `SpendingPlanCharts.tsx` 697, `TransactionsTable.tsx` 558)
- Warning/lint suppressions: 3 (all `react-hooks/exhaustive-deps`), 0 `@ts-ignore`/`@ts-expect-error`, 2 `as unknown as`, C# pragmas only in generated migrations
- `any` usages: 0
- Estimated duplication: ~95% between the SpendingPlan Income vs Expense services/controllers/repos/DTOs

## Findings

### 1. SpendingPlan Income/Expense vertical is duplicated end-to-end
- **Category:** DRY
- **Description:** The income and expense line-item features are near-byte-identical, differing only by the `Income`↔`Expense` token. `SpendingPlanIncomeService` (274 lines) and `SpendingPlanExpenseService` (274 lines) differ by ~70 token-swap lines; the controllers, repositories+interfaces, request DTOs, and query params are likewise cloned. `Create/Update/Patch/Delete/Get`, `ResolveLineItem`, and `VerifySpendingPlanAsync` bodies are identical. Any bug fix or feature must be made twice and kept in sync.
- **Location:** `Cairnly.API/Services/Domain/SpendingPlanIncomeService.cs` & `SpendingPlanExpenseService.cs`; `Cairnly.API/Controllers/V1/SpendingPlanIncomesController.cs` & `SpendingPlanExpensesController.cs`; `Cairnly.API/Data/Repositories/SpendingPlan{Income,Expense}Repository.cs`; `Cairnly.API/Models/Requests/SpendingPlan{Incomes,Expenses}/*.cs`
- **Impact:** High
- **Effort:** High (4-8hr)
- **Dependencies:** None
- **Breaking Changes:** No — internal refactor; public routes/DTOs unchanged
- **Recommendation:** Introduce a generic `SpendingPlanLineItemService<TEntity,TTag,TDto,TCreate,TUpdate>` base (and matching repository base) holding the shared CRUD/validation/authorization flow; keep thin `Income`/`Expense` subclasses for entity-specific factory deltas. Collapses ~95% of ~550 duplicated lines.

### 2. `AuthController` is a 668-line, 10-dependency god controller
- **Category:** Architecture / Size
- **Description:** One controller owns register, password login, Google login, GitHub login, logout, refresh, both OAuth start/callback pairs, plus private token issuance, cookie minting, and OAuth-flow crypto helpers — 10 constructor dependencies (the SRP ceiling). Token+cookie issuance (`GenerateAndSaveAccessAndRefreshTokensAsync`) and OAuth flow handling belong in services. There is also a misspelled field `dnumerationErrorCodes` (should be `enumeration…`) at lines 52 & 138 hurting readability.
- **Location:** `Cairnly.API/Controllers/V1/AuthController.cs:37` (10 ctor deps at 82-105), `:563` token/cookie issuance, `:610` BeginOAuthFlow, `:52`/`:138` typo
- **Impact:** Medium
- **Effort:** High (4-8hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Split into `AuthController` (register/login/logout/refresh) and `OAuthController` (start/callback/social login); extract token+cookie issuance and OAuth-flow cookie crypto into a service to drop dep count; rename `dnumerationErrorCodes`.

### 3. `LineItemsSection.tsx` is 760 lines with 7 components in one file
- **Category:** Frontend / Size
- **Description:** Far over the 200-line component guideline and bundles 7 components (`LineItemsSection`, `LineItemsTable`, `SortableColumn`, `EditableNameCell`, `EditableCategoryCell`, `DetailsModal`, `TableFooter`) — deviating from the one-file-per-concern split. Mixes data orchestration, inline-edit, search/sort/paginate, and presentation, making it hard to review and test in isolation.
- **Location:** `cairnly-ui/src/components/spendingPlans/LineItemsSection.tsx` (functions at lines 370, 404, 484, 534, 601, 667)
- **Impact:** Medium
- **Effort:** Medium (1-4hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Extract `LineItemsTable`, the editable cells, `DetailsModal`, and `TableFooter` into sibling files; keep `LineItemsSection` as the orchestrator under ~200 lines.

### 4. `SpendingPlanCharts.tsx` is 697 lines mixing two chart families + unsafe casts
- **Category:** Frontend / Size / Unsafe
- **Description:** Combines composition donuts and the cash-flow Sankey (DonutCard/DonutTooltip + CashFlowSankeyCard/SankeyFlowNode/Link/Tooltip) plus all transform helpers in one file, with two `as unknown as` casts coercing untyped recharts payloads (lines 684, 717).
- **Location:** `cairnly-ui/src/components/spendingPlans/SpendingPlanCharts.tsx` (Sankey block ~615-760, casts at 684 & 717)
- **Impact:** Medium
- **Effort:** Medium (1-4hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Split donut vs Sankey into separate files (shared transforms in `utils`); replace `as unknown as` with a typed payload narrowing helper/guard.

### 5. Inline editable-cell + request-builder components duplicated across tables
- **Category:** DRY / Frontend
- **Description:** The two tables independently reimplement the same inline-edit cells and full-request builders: `EditableCategoryCell` (LineItemsSection:484, TransactionsTable:490), name vs merchant cells (404 vs 410), and the full-request-from-item-plus-patch builder (`handleInlineSave` LineItemsSection:188 vs `buildUpdateRequest` TransactionsTable:51). Editing behavior must be fixed in two places.
- **Location:** `cairnly-ui/src/components/spendingPlans/LineItemsSection.tsx:188,484` & `cairnly-ui/src/components/transactions/TransactionsTable.tsx:51,490`
- **Impact:** Medium
- **Effort:** Medium (1-4hr)
- **Dependencies:** Pairs with #3 (extract cells once during that split)
- **Breaking Changes:** No
- **Recommendation:** Promote a shared `EditableCategoryCell` and a generic `<EditableTextCell>` into `components/`, and centralize the patch-merge builder, so both tables consume one implementation.
