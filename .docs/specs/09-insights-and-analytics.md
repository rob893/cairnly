# Pillar 9 — Insights & Analytics

> Surface meaningful trends.

← [Back to overview](./README.md)

## Summary

Move from data to understanding. Cairnly proactively surfaces the patterns a user would miss in a
table — spending spikes, creeping costs, and how income and expenses diverge over years.

## User value / outcomes

- Automatic "you should know this" callouts instead of manual analysis.
- Long-range perspective (YoY, multi-year) on habits and trajectory.
- Early detection of lifestyle creep and anomalies.

## Examples of insights

- Lifestyle creep detection.
- Year-over-year spending comparisons.
- Largest spending categories.
- Budget pacing.
- Savings-rate trends.
- Unexpected spending spikes.
- Monthly summaries.
- Annual summaries.

## Example interactions

- "**Home insurance increased 17%.**"
- "**Amazon spending is unusually high this month.**"
- "**Expenses have grown faster than income over the last 2 years.**"

## Maps to the stack (light)

- **Entities:** read-only over `Transaction`, `Category`, `Budget`, `NetWorthSnapshot`,
  `RecurringSeries`; optional `Insight` records to persist generated callouts (so they can be
  dismissed/snoozed).
- **API (`/api/v1`):**
  - `GET /insights` (current callouts, ranked by relevance/severity).
  - `POST /insights/{id}/dismiss`.
  - `GET /reports/summary?period=monthly|annual`.
- **Compute:** a periodic analytics job evaluates detectors (anomaly vs. baseline, YoY deltas,
  trend regressions) and emits insights; keep each detector small and testable.
- **UI:** an insights feed/cards on the [Dashboard (4)](./04-financial-dashboard.md), plus monthly
  and annual summary reports.
- **Dependencies:** quality depends on clean data from [Rules Engine (8)](./08-rules-engine.md) and
  [Recurring Detection (7)](./07-recurring-transaction-detection.md); consumes
  [Net Worth (2)](./02-net-worth-tracking.md) and [Budgeting (1)](./01-spending-and-budgeting.md).

## Open questions / future

- Baseline/seasonality handling so normal seasonal swings aren't flagged as anomalies.
- Ranking/notification strategy to avoid insight fatigue.
- Which insights are real-time vs. batch, and how far back history must go to be meaningful.
