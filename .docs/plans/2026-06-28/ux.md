# UX Research — 2026-06-28

## Executive Summary

Cairnly's authenticated app is well-built on HeroUI v3 with disciplined theme-token usage and a strong, consistent brand on the Landing/Login/Register surfaces. The biggest gaps are about *trust feedback* and *consistency on secondary surfaces*: the app gives users no success confirmation (or screen-reader announcement) after money actions, and the password-reset/error surfaces diverge from the brand system with leftover HeroUI-v2 tokens and emoji. Below are the 5 most impactful new findings, ranked by impact.

## Status

| #   | Finding                                                    | Impact | Effort | Status       |
| --- | --------------------------------------------------------- | ------ | ------ | ------------ |
| 1   | No success feedback or live-region after mutations        | High   | Medium | ⬜ Pending   |
| 2   | Forgot/Reset password bypass branded AuthShell            | High   | Medium | ⬜ Pending   |
| 3   | Error components use emoji + dead tokens                   | Medium | Low    | ✅ Done (W1) |
| 4   | DateField visually inconsistent with HeroUI inputs        | Low    | Low    | ✅ Done (W1) |
| 5   | Empty states styled three different ways                  | Medium | Low    | ✅ Done (W1) |

## Findings

### 1. No success feedback or live-region after any mutation
- **Description:** Saving a transaction, creating/editing/deleting an account, deleting a spending plan, reconciling a balance, and updating profile/preferences all complete *silently* — the modal just closes. There is no toast/notification system (no `ToastProvider` in `main.tsx`, zero `toast`/`addToast` usages) and zero `aria-live`/`role="status"` regions anywhere, so screen-reader users get no confirmation that money changed and sighted users get no reassurance. For a "calm, trustworthy" finance app, silent state changes around money undercut confidence and make failures vs. successes ambiguous. The only feedback is error cards (`ApiErrorDisplay`).
- **Location:** `cairnly-ui/src/main.tsx:31-44` (no provider); `cairnly-ui/src/components/accounts/SetBalanceModal.tsx:53-69`; `cairnly-ui/src/components/transactions/TransactionsTable.tsx:194-212` and inline cells `435-450`; `cairnly-ui/src/pages/AccountsPage.tsx:109-127`; `cairnly-ui/src/components/spendingPlans/SpendingPlansSection.tsx:73-81`; `cairnly-ui/src/components/settings/ProfileSection.tsx:47-53`. App-wide search for `toast|aria-live|role="status"` returns nothing.
- **Impact:** High
- **Effort:** Medium (1-4hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Add a single global notification mechanism: a `ToastProvider` near the QueryClient in `main.tsx` (use HeroUI's toast if available in 3.2.1, else a small `fixed bottom-4` region with `role="status" aria-live="polite"`). Fire a calm success toast on each `mutateAsync` resolve (e.g., "Balance updated", "Transaction saved", "Account deleted"). Announce balance/net-worth refreshes via the same polite live-region so saves are perceivable without sight.

### 2. Forgot/Reset Password pages bypass the branded AuthShell and use undefined legacy tokens
- **Description:** Login and Register use the polished split-screen `AuthShell` (cairn motif, aurora, accent), but `ForgotPasswordPage` and `ResetPasswordPage` render a plain centered card with `bg-linear-to-br from-background to-content1`, a text-only "Cairnly" wordmark, emoji status (✅/❌), and HeroUI-v2 tokens (`text-primary`, `to-content1`, `text-default-600/500`, `primary-600`, `bg-default-50`) that are **not defined** in `index.css` (theme uses `--accent`/`--surface`/`--default-foreground`, not `primary`/`content1`/numeric scale). Those classes fall back to unstyled/wrong colors, so these flows look broken and off-brand in dark mode — a jarring drop mid-journey for a security-sensitive task.
- **Location:** `cairnly-ui/src/pages/ForgotPasswordPage.tsx:29-114` (e.g. `:32` `text-primary`, `:29/69` `to-content1`, `:35` `✅`, `:106` `primary-600`); `cairnly-ui/src/pages/ResetPasswordPage.tsx:74-189` (e.g. `:101` `text-primary`, `:103` `❌`, `:131` `bg-default-50`). Contrast: `AuthShell` consumed only by `LoginPage.tsx:39` and `RegisterPage.tsx:67`.
- **Impact:** High
- **Effort:** Medium (1-4hr)
- **Dependencies:** Shares the token-cleanup work in Finding 3
- **Breaking Changes:** No
- **Recommendation:** Refactor both pages onto `AuthShell` (single-column variant) for brand parity. Replace legacy tokens with theme tokens (`text-foreground`/`text-muted`, `bg-background`, `text-accent`) and swap ✅/❌ chips for lucide `MailCheck`/`AlertCircle`. Keep the security copy.

### 3. Shared error components use emoji and undefined legacy tokens instead of the lucide/theme system
- **Description:** `ApiErrorDisplay` — rendered on virtually every error path (dashboard, accounts, transactions, all modals) — leads with a `❌` emoji and uses `text-default-600`/`text-default-500` (undefined in theme). `ErrorBoundary` uses `text-primary`, `bg-linear-to-br ... to-content1`, `⚠️`, and `text-default-600`. `ProfileSection` shows `✓ Verified` as a glyph. The rest of the app standardizes on lucide-react icons and `--danger`/`--muted` tokens, so these emoji + dead tokens read as inconsistent and slightly unpolished on the exact screens where trust matters most (failures).
- **Location:** `cairnly-ui/src/components/ApiErrorDisplay.tsx:18-19` (`❌`), `:43-48` (`text-default-600/500`); `cairnly-ui/src/components/ErrorBoundary.tsx:50` (`to-content1`), `:53` (`text-primary`), `:55` (`⚠️`), `:61` (`text-default-600`); `cairnly-ui/src/components/settings/ProfileSection.tsx:121` (`✓ Verified`).
- **Impact:** Medium
- **Effort:** Low (<1hr)
- **Dependencies:** None (define one mapping reused by Finding 2)
- **Breaking Changes:** No
- **Recommendation:** Replace `❌`/`⚠️`/`✓` with lucide `AlertCircle`/`TriangleAlert`/`CheckCircle2` tinted via `text-danger`/`text-success`; replace `text-default-500/600` with `text-muted`, `to-content1` with `to-surface`, `text-primary` with `text-accent`. Mirror this fix in the auth pages.

### 4. Custom date field is visually inconsistent with HeroUI text inputs in the same form
- **Description:** `DateField` hard-codes its wrapper as `rounded-lg border border-border bg-surface` (radius `--radius` 0.5rem), while the HeroUI `Input` used by `FormField` uses `--field-radius` (0.75rem) and `--field-background`. When stacked in a modal these controls have visibly different corner radii and fill colors, so the date input looks like a different widget than the amount/text inputs right beside it — most noticeable on the reconciliation modal where "As of" and "Balance" sit together.
- **Location:** `cairnly-ui/src/components/DateField.tsx:56`; vs `cairnly-ui/src/components/FormField.tsx:46`. Seen together in `cairnly-ui/src/components/accounts/SetBalanceModal.tsx:93,95-101` and `AccountFormModal` / `TransactionFormModal`.
- **Impact:** Low
- **Effort:** Low (<1hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Align the `DateField` Group to field tokens: `rounded-[var(--field-radius)] bg-[var(--field-background)]` and match Input padding/focus so date and text fields are pixel-consistent.

### 5. Empty states are styled three different ways; the global Transactions empty state is weakest
- **Description:** Empty/zero states aren't standardized: dashboard and spending plans use a centered `Card` with a CTA, accounts uses a hand-rolled `rounded-2xl border bg-surface` block with a button, but the Transactions table renders a bare `<p>No transactions yet. Add one to get started.</p>` with no card, icon, or action. A first-time user opening Transactions sees a tiny muted sentence and must find the top-bar add button — the least guided moment in an otherwise tidy app.
- **Location:** Bare: `cairnly-ui/src/components/transactions/TransactionsTable.tsx:237`. Card+CTA: `cairnly-ui/src/pages/HomePage.tsx:104-117`, `cairnly-ui/src/components/spendingPlans/SpendingPlansSection.tsx:33-42`. Manual block: `cairnly-ui/src/pages/AccountsPage.tsx:149-155`.
- **Impact:** Medium
- **Effort:** Low (<1hr)
- **Dependencies:** None
- **Breaking Changes:** No
- **Recommendation:** Add one shared `EmptyState` (icon + title + sub + optional CTA) and use it everywhere, including the Transactions table (with an "Add transaction" CTA) so radius/CTA presence is consistent and first-run is guided.
