# Cairnly — Feature Spec

> **Financial clarity for the long journey.**

Cairnly helps people understand, plan, and optimize their financial lives by treating personal
finances like an **observable system** rather than a spreadsheet. Inspired by trail **cairns** — the
stacked stone markers that show hikers they're on the right path — the platform focuses on
**progress, milestones, and long-term wealth building**.

This folder is the high-level product/feature specification. It is intentionally **product-first**;
each pillar carries a *light* "maps to the stack" note so engineering has a starting point, but
nothing here is a binding technical design.

## How to read this spec

- This `README.md` is the **overview**: vision, the shared domain model, the phased roadmap, and
  cross-cutting concerns.
- Each **pillar** has its own file (linked below) with features, example insights, a light technical
  mapping, dependencies, and open questions.
- Audience is **mixed** (product + engineering). Treat technical notes as suggestions, not commitments.

## The cairn metaphor

A cairn is built one stone at a time, and each marker confirms forward progress on a long trail.
Cairnly applies the same idea to money: small consistent actions (logging spend, funding goals)
accumulate into **milestones** (Pillar 11) — the markers that prove you're moving toward financial
independence. Progress and momentum are first-class, not afterthoughts.

## Pillars at a glance

| # | Pillar | Outcome | Spec |
|---|--------|---------|------|
| 1 | Spending & Budgeting | Know where money goes; plan vs. actual | [01](./01-spending-and-budgeting.md) |
| 2 | Net Worth Tracking | Watch wealth accumulate over time | [02](./02-net-worth-tracking.md) |
| 3 | Tax Awareness | Estimate after-tax income & burden | [03](./03-tax-awareness.md) |
| 4 | Financial Dashboard | High-level observability | [04](./04-financial-dashboard.md) |
| 5 | Goals | Track progress toward objectives | [05](./05-goals.md) |
| 6 | Cash Flow Forecasting | See upcoming income & expenses | [06](./06-cash-flow-forecasting.md) |
| 7 | Recurring Transaction Detection | Surface subscriptions & repeats | [07](./07-recurring-transaction-detection.md) |
| 8 | Rules Engine | Auto-organize imported transactions | [08](./08-rules-engine.md) |
| 9 | Insights & Analytics | Surface meaningful trends | [09](./09-insights-and-analytics.md) |
| 10 | Financial Independence Planning | Project long-term outcomes | [10](./10-financial-independence-planning.md) |
| 11 | Milestones | Mark the journey with cairns | [11](./11-milestones.md) |

## Cross-cutting domain model

These core entities are shared across pillars. (Names are indicative; final schema is TBD.)

| Entity | Purpose | Key fields (indicative) | Used by |
|--------|---------|-------------------------|---------|
| `Account` | A financial account (asset or liability) | `id`, `name`, `type` (checking/savings/brokerage/retirement/HSA/cash/real-estate/vehicle/crypto/mortgage/credit-card/loan), `class` (asset/liability), `currency`, `currentBalance`, `isManual` | 2, 4, 6 |
| `Transaction` | A single money movement | `id`, `accountId`, `date`, `amount` (signed minor units), `merchant`, `description`, `categoryId`, `tagIds`, `recurringSeriesId?`, `source` (manual/import), `isSplit`, `parentTransactionId?` | 1, 4, 6, 7, 8, 9 |
| `Category` | Grouping for spend/income (system + user-defined) | `id`, `name`, `parentId?`, `kind` (income/expense/transfer), `isSystem` | 1, 8, 9 |
| `Tag` | Free-form label on a transaction | `id`, `name` | 1, 8, 9 |
| `Budget` | A spending plan for a category over a period | `id`, `categoryId`, `period` (daily/weekly/monthly/annual), `amount`, `rollover` (envelope) | 1, 4, 9 |
| `Rule` | Condition → action for organizing transactions | `id`, `priority`, `conditions[]`, `actions[]`, `enabled` | 7, 8 |
| `RecurringSeries` | A detected/confirmed repeating charge | `id`, `merchant`, `cadence`, `expectedAmount`, `lastSeen`, `nextExpected`, `status` | 6, 7, 9 |
| `Goal` | A funding objective with a target | `id`, `name`, `targetAmount`, `currentAmount`, `targetDate?`, `linkedAccountId?`, `status` | 5, 6, 11 |
| `NetWorthSnapshot` | Point-in-time aggregate of assets/liabilities | `id`, `asOf`, `totalAssets`, `totalLiabilities`, `netWorth` | 2, 4, 9, 11 |
| `TaxProfile` | Inputs for tax estimation | `id`, `filingStatus`, `state`, `salary`, `bonus`, `rsuValue`, `pretax401k`, `hsa` | 3, 4 |
| `FiPlan` | Assumptions for FI projections | `id`, `expectedReturn`, `inflation`, `savingsRate`, `targetRetireAge`, `withdrawalRate` | 10, 11 |
| `Milestone` | A celebrated marker on the journey | `id`, `type`, `achievedAt?`, `target`, `status` | 11 |

All financial amounts are stored as **integer minor units** (e.g., cents) with an explicit
`currency` to avoid floating-point drift.

## Phased roadmap

Phases are sequenced to deliver "clarity" first, then automation, then long-horizon planning. Pillar
numbers in parentheses.

- **Phase 0 — Foundation**
  Accounts, manual transactions, categories & tags, multi-currency basics, per-user data isolation.
  *(Underpins everything; reuses the template's auth + repository/data patterns.)*
- **Phase 1 — MVP: core clarity**
  Spending & Budgeting (1), Net Worth Tracking (2), Financial Dashboard (4).
- **Phase 2 — Automation & organization**
  CSV import, Recurring Transaction Detection (7), Rules Engine (8), Goals (5).
- **Phase 3 — Forecasting & awareness**
  Cash Flow Forecasting (6), Insights & Analytics (9), Tax Awareness (3).
- **Phase 4 — Long-horizon**
  Financial Independence Planning (10), Milestones (11), scenario / what-if modeling.

## Cross-cutting concerns

- **Privacy & sensitivity.** Financial data is highly sensitive. Everything is scoped per user
  (multi-tenant by `UserId`); never expose another user's data. Prefer aggregates over raw exports in
  shared/log surfaces. No transaction detail in correlation/trace logs.
- **Auth & ownership.** Builds on the existing ASP.NET Identity + JWT foundation; every domain entity
  is owned by a user (reuse the `IOwnedByUser` pattern). Global `[Authorize]`; opt out only for
  truly public endpoints.
- **Money & precision.** Integer minor units + currency everywhere; one place for rounding rules.
  FX/multi-currency normalization for net-worth rollups is an explicit concern.
- **Time & periods.** Budgets, dashboards, and forecasts share period semantics
  (daily/weekly/monthly/annual, plus YoY). Define the user's week-start and fiscal boundaries once.
- **Observability.** Lean on the API's correlation IDs and App Insights/OpenTelemetry. Cairnly's
  product thesis is "finances as an observable system" — that applies to the platform itself too.
- **Performance.** Large transaction histories → cursor-based pagination (`CursorPaginatedList<T>`)
  and indexed filters; pre-aggregate snapshots (net worth, monthly rollups) rather than recomputing.
- **Estimates are estimates.** Tax and FI outputs are projections, not advice — label them clearly.

## Technical foundation (shared)

These specs assume the existing template stack:

- **API** — `Cairnly.API` (.NET 10, ASP.NET Core), EF Core + PostgreSQL, repository pattern
  (`Repository<>`/`IRepository<>`), service-result pattern (`Result<T>`), cursor pagination
  (`CursorPaginatedList<T>`), URL-segment versioning (`/api/v1/…`).
- **UI** — `cairnly-ui` (React 19 + Vite + TypeScript + Tailwind v4 + HeroUI v3), TanStack Query for
  server state, shared `apiClient`.
- **Infra** — Azure App Service (API) + GitHub Pages (UI), App Insights, Key Vault, Postgres Flexible
  Server (Bicep under `CI/Azure`).

## Glossary

- **Burn rate** — average spend per unit time; how fast cash is consumed.
- **Cairn / Milestone** — a celebrated marker of financial progress (see Pillar 11).
- **Coast FI** — having enough invested that, with no further contributions, you'll reach FI by your
  target age purely through growth.
- **Effective tax rate** — total tax ÷ total income.
- **Envelope budgeting** — allocating money into category "envelopes"; unspent amounts may roll over.
- **FI number** — the portfolio size at which work becomes optional (commonly annual expenses ÷
  withdrawal rate).
- **Lifestyle creep** — spending rising in step with (or faster than) income over time.
- **Net worth** — total assets minus total liabilities.
- **Savings rate** — share of income not spent (saved/invested) over a period.
- **Snapshot** — a stored point-in-time aggregate (e.g., daily/monthly net worth).
