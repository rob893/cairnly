# Pillar 4 — Financial Dashboard

> High-level observability into personal finances.

← [Back to overview](./README.md)

## Summary

The home base. A single screen that answers "how am I doing?" at a glance by aggregating the other
pillars into a small set of trusted metrics across time. This is the clearest expression of Cairnly's
"finances as an observable system" thesis.

## User value / outcomes

- A 10-second read on financial health.
- Consistent metrics that mean the same thing everywhere in the app.
- Easy zoom between today, this month, and the year.

## Metrics

- Income
- Expenses
- Cash flow
- Savings rate
- Net worth
- Monthly surplus
- Burn rate
- Budget utilization

## Time views

Daily · Weekly · Monthly · Annual · Year-over-year.

## Maps to the stack (light)

- **Entities:** read-only; composes `Transaction`, `Budget`, `NetWorthSnapshot`, `Account`.
- **API (`/api/v1`):**
  - `GET /dashboard/summary?period=monthly&compare=yoy` (one call returning the metric set for the
    selected period + comparison deltas).
  - May fan out to per-pillar read models, or read from pre-aggregated rollups for speed.
- **Read models:** prefer materialized monthly rollups (income/expense/savings) so the dashboard is a
  cheap read; recompute on transaction changes rather than on every page load.
- **UI:** metric cards with trend sparklines, a period switcher (daily→annual + YoY toggle), and
  drill-through links into the owning pillar.
- **Dependencies:** consumes [Budgeting (1)](./01-spending-and-budgeting.md),
  [Net Worth (2)](./02-net-worth-tracking.md), [Tax (3)](./03-tax-awareness.md),
  [Cash Flow (6)](./06-cash-flow-forecasting.md), and surfaces
  [Insights (9)](./09-insights-and-analytics.md).

## Open questions / future

- Exact metric definitions (e.g., savings rate = (income − expenses) ÷ income, gross or net of tax?)
  — must be defined once and reused everywhere.
- Configurable/rearrangeable dashboard widgets vs. a fixed opinionated layout for MVP.
- How "burn rate" treats irregular/large one-off expenses.
