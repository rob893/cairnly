# Master Research Plan — 2026-06-28

## Executive Summary

Five parallel research agents (performance, security, quality, UX, features) swept the Cairnly
codebase. The verdict is consistent: the foundation is genuinely strong — no Critical/High security
holes, no blocking backend calls, snapshot-backed balances with proper indexes, disciplined theme
tokens, zero `any`/`@ts-ignore`. The 23 findings cluster into three themes:

1. **Trust feedback gaps (UX/Sec):** silent money mutations with no toast/live-region, plus a few
   Medium security hardening gaps (Google audience pin, PII in logs, rate-limit bucket).
2. **Scale & polish (Perf/Quality):** the transactions list drains all pages serially and isn't
   virtualized; income/expense backend stack is ~95% duplicated; a handful of oversized components.
3. **Feature reality (Features):** the marquee "clarity" surfaces are stubs — Cash Flow is 100% mock,
   no budget-vs-actual, goals, recurring detection, or CSV import.

Recommended priority: knock out the low-effort security + UX consistency fixes first, then the
transactions perf fix and global toasts, then refactor before building the big features (de-dupe the
line-item stack and ship real Cash Flow aggregation before cloning it into Budgets).

## Status Overview

| Wave | Items                                   | Status         |
| ---- | --------------------------------------- | -------------- |
| 1    | S1, S2, S3, S4, P2, U3, U4, U5 (8)      | ✅ Complete    |
| 2    | U1, U2, P1, P3, S5, P4 (6)              | ✅ Complete    |
| 3    | Q1, Q3+Q5, Q4, Q2 (4)                   | ✅ Complete    |
| 4    | F1, F5, F3, F4, F2 (5)                  | ⬜ Pending     |

## Execution Waves

### Wave 1: Critical / No Dependencies (quick, high-trust, low-effort) — ✅ COMPLETE

| #   | Area     | Finding                                                        | Effort | Impact | Status  |
| --- | -------- | -------------------------------------------------------------- | ------ | ------ | ------- |
| 1   | Security | S1 — Pin Google ID-token audience + startup guard              | Low    | Medium | ✅ Done |
| 2   | Security | S2 — Stop logging PII (emails) in recovery flows; log UserId   | Low    | Medium | ✅ Done |
| 3   | Security | S3 — Put anonymous forgot/reset password in strict rate bucket | Low    | Low    | ✅ Done |
| 4   | Security | S4 — Clear csrf/oauth cookies on logout; shorten JWT lifetime  | Low    | Low    | ✅ Done |
| 5   | Perf     | P2 — Lazy-load dashboard chart cards (~121 KB gzip off paint)  | Low    | Medium | ✅ Done |
| 6   | UX       | U3 — Replace emoji/legacy tokens with lucide + theme tokens    | Low    | Medium | ✅ Done |
| 7   | UX       | U5 — Shared `EmptyState` (esp. Transactions); guided first-run | Low    | Medium | ✅ Done |
| 8   | UX       | U4 — Align `DateField` to `--field-radius`/`--field-background`| Low    | Low    | ✅ Done |

### Wave 2: High Priority / Minimal Dependencies — ✅ COMPLETE

| #   | Area     | Finding                                                            | Effort | Impact | Status  |
| --- | -------- | ----------------------------------------------------------------- | ------ | ------ | ------- |
| 9   | UX       | U1 — Global toast + `aria-live` success feedback on mutations     | Medium | High   | ✅ Done |
| 10  | UX       | U2 — Refactor Forgot/Reset password onto branded `AuthShell`      | Medium | High   | ✅ Done |
| 11  | Perf     | P1 — On-demand paging + virtualize TransactionsTable              | Medium | High   | ✅ Done |
| 12  | Perf     | P3 — Batch `GET /spending-plans/summaries` endpoint               | Medium | Medium | ✅ Done |
| 13  | Security | S5 — Host `frame-ancestors`/X-Frame-Options; CORS header allowlist| Medium | Low    | ✅ Done |
| 14  | Perf     | P4 — HeroUI deep imports (verified supported, no measurable win)  | Medium | Medium | ✅ Done |

### Wave 3: Quality Refactors — ✅ COMPLETE

| #   | Area     | Finding                                                          | Effort    | Impact | Status  |
| --- | -------- | ---------------------------------------------------------------- | --------- | ------ | ------- |
| 15  | Quality  | Q1 — Generic SpendingPlan line-item base (de-dupe ~550 lines)    | High      | High   | ✅ Done |
| 18  | Quality  | Q3 + Q5 — Split `LineItemsSection`; promote shared editable cells| Medium    | Medium | ✅ Done |
| 19  | Quality  | Q4 — Split `SpendingPlanCharts`; type recharts payloads          | Medium    | Medium | ✅ Done |
| 20  | Quality  | Q2 — Split `AuthController` → Auth/OAuth; extract token service   | High      | Medium | ✅ Done |

### Wave 4: Major Features (build on Wave 3; F1/F5 deferred from W3)

| #   | Area     | Finding                                            | Effort    | Impact   | Dependencies   |
| --- | -------- | -------------------------------------------------- | --------- | -------- | -------------- |
| 16  | Features | F1 — Real Cash Flow & trends API; retire mock data | High      | High     | None           |
| 17  | Features | F5 — CSV transaction import (staged → confirm)     | High      | High     | None           |
| 21  | Features | F3 — Goals (progress + projected completion)       | High      | High     | None           |
| 22  | Features | F4 — Recurring / subscription detection            | Very High | High     | F5 (boosts data)|
| 23  | Features | F2 — Budgets (per-category limits, budget-vs-actual)| Very High | Critical | F1, Q1         |

## Cross-Dependency Map

| Finding                      | Must come after | Why                                                              |
| ---------------------------- | --------------- | ---------------------------------------------------------------- |
| U2 reset-pwd AuthShell       | U3              | Reuses the same legacy-token → theme-token mapping               |
| Q5 shared cells              | Q3              | Cells extracted during the `LineItemsSection` split              |
| F2 Budgets                   | F1, Q1          | Reuses cash-flow category rollups; clone the *de-duped* LI stack |
| F4 Recurring                 | F5 (soft)       | Import boosts transaction volume; works on manual data too       |
| Goals projection (later)     | F1 (soft)       | Cash-flow surplus feeds "funding capacity"                       |

Order matters most for: **Q1 before F2** (don't clone a duplicated stack), **F1 before F2** (shared
rollups), **U3 before U2** (token mapping). Everything in Waves 1–2 is independent and parallelizable.

## Previous Plan Status

No previous plan directory existed. All 23 findings are new this cycle.

## Wave 1 Implementation Log (2026-06-28)

All 8 Wave 1 items implemented via 3 parallel sub-agents; API build + 161 tests pass, UI lint + build pass. No commits made.

- **S1** — `GoogleOAuthService.ResolveGoogleOAuthAudiences` defaults to client id; startup guard in `AuthenticationServiceCollectionExtensions` fails fast non-dev if empty.
- **S2** — `UserService` recovery flows log `user.Id` (or nothing on not-found), never emails.
- **S3** — `RateLimiterServiceCollectionExtensions.IsStrictAuthRateLimitedPath` covers forgot/reset/email-confirm.
- **S4** — `AuthController.LogoutAsync` deletes csrf+oauth cookies; `TokenExpirationTimeInMinutes` 60→15.
- **P2** — `HomePage` lazy-loads chart cards behind Suspense.
- **U3** — emoji/legacy v2 tokens → lucide icons + theme tokens in `ApiErrorDisplay`/`ErrorBoundary`/`ProfileSection`.
- **U4** — `DateField` uses `--field-radius`/`--field-background`.
- **U5** — new shared `EmptyState` used in Transactions/Home/SpendingPlans/Accounts.

## Wave 2 Implementation Log (2026-06-28)

All 6 Wave 2 items implemented via parallel sub-agents; API build + 165 tests pass, UI lint + build + 118 tests pass. No commits made. The W1+W2 hardening fixes (S1–S4) were also ported to the sibling reference repos `derpcode` and `starter-app-template`.

- **U1** — global `ToastProvider` in `main.tsx` + `utils/notifications.ts`; success/`aria-live` toasts on transactions, accounts, balance reconcile, spending-plan, and profile/preferences mutations.
- **U2** — `ForgotPasswordPage`/`ResetPasswordPage` refactored onto branded `AuthShell` with theme tokens + lucide icons.
- **P1** — `TransactionsTable` on-demand paging (no eager drain) + `@tanstack/react-virtual` row virtualization.
- **P3** — `GET /spending-plans/summaries` batch endpoint; `HomePage` uses one `useQuery`.
- **S5** — `SecurityHeadersMiddleware` adds `X-Frame-Options: DENY` + `frame-ancestors 'none'`; CORS header allowlist (Authorization, Content-Type, X-CSRF-Token, X-Correlation-Id).
- **P4** — verified HeroUI v3 deep imports supported but no measurable size win (~0.5 KB gzip); left as-is.

## Wave 3 Implementation Log (2026-06-28)

Four quality refactors implemented via parallel sub-agents; API build + 170 tests pass, UI build + 125 tests pass. No breaking changes, no commits. F1/F5 deferred to Wave 4.

- **Q1** — generic SpendingPlan line-item service + repository bases; Income/Expense now thin subclasses (~550 duplicated lines collapsed). Routes/DTOs unchanged.
- **Q2** — split `AuthController` (10→6 deps) + new `OAuthController`; extracted token/cookie + oauth-flow services; fixed `dnumerationErrorCodes` typo.
- **Q3+Q5** — `LineItemsSection` 760→198 lines via sibling extraction; shared `EditableTextCell`/`EditableCategoryCell` + `patchRequest` consumed by both tables.
- **Q4** — `SpendingPlanCharts` 697→140 lines (DonutCard/CashFlowSankeyCard/utils); 0 `as unknown as` casts remain.

## Area Plans

- [performance.md](./performance.md) — 4 findings
- [security.md](./security.md) — 5 findings
- [quality.md](./quality.md) — 5 findings
- [ux.md](./ux.md) — 5 findings
- [features.md](./features.md) — 5 findings
