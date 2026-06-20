# Pillar 2 — Net Worth Tracking

> Track wealth accumulation over time.

← [Back to overview](./README.md)

## Summary

Aggregate every asset and liability into a single number — net worth — and watch it move over time.
This is the long-horizon counterpart to day-to-day budgeting and the primary source of progress for
[Milestones (11)](./11-milestones.md).

## User value / outcomes

- One honest number that answers "how am I doing overall?"
- A history that shows the trend, not just today's balance.
- Visibility into how wealth is allocated and how debt is shrinking.

## Assets

Checking · Savings · Brokerage · Retirement accounts · HSA · Cash · Real estate · Vehicles · Crypto.

## Liabilities

Mortgage · Credit cards · Student loans · Car loans.

## Features

- Account balances (manual entry; import-assisted where possible).
- Daily/monthly snapshots.
- Net worth history.
- Asset allocation breakdown.
- Debt tracking.
- Long-term wealth charts.

## Milestones (surfaced here, owned by Pillar 11)

First $10k · First $100k · First $500k · First $1M · Debt-free · Mortgage payoff · Financial
Independence. See [Milestones (11)](./11-milestones.md).

## Maps to the stack (light)

- **Entities:** `Account` (asset/liability `class`), `NetWorthSnapshot`.
- **API (`/api/v1`):**
  - `GET/POST/PUT/DELETE /accounts`; `POST /accounts/{id}/balance` (record a balance update).
  - `GET /net-worth` (current rollup, allocation breakdown).
  - `GET /net-worth/history?from=…&to=…&grain=monthly` (snapshot series).
- **Snapshots:** a scheduled job materializes `NetWorthSnapshot` daily/monthly so history charts and
  milestone checks are cheap reads rather than recomputations.
- **UI:** net-worth headline + trend chart, allocation donut, account list grouped by class,
  per-account balance history.
- **Dependencies:** feeds [Dashboard (4)](./04-financial-dashboard.md),
  [Insights (9)](./09-insights-and-analytics.md), [FI Planning (10)](./10-financial-independence-planning.md),
  and [Milestones (11)](./11-milestones.md).

## Open questions / future

- Valuation of illiquid assets (real estate, vehicles) — manual, or pluggable estimators?
- Multi-currency net worth: normalization currency and FX source.
- Snapshot cadence/backfill and how manual balance edits reconcile with derived balances.
- Crypto/brokerage price refresh — out of scope for MVP (manual balances first)?
