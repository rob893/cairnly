# Pillar 7 — Recurring Transaction Detection

> Automatically identify repeating expenses.

← [Back to overview](./README.md)

## Summary

Find the subscriptions and bills hiding in the transaction stream. Detecting recurring charges powers
subscription awareness, price-increase alerts, and the inputs that
[Cash Flow Forecasting (6)](./06-cash-flow-forecasting.md) needs to look ahead.

## User value / outcomes

- A clear list of every recurring charge and what it costs per month.
- Alerts when a subscription quietly raises its price.
- Reliable inputs for forecasting.

## Examples

Netflix · Spotify · Internet · Electric · Mortgage · Insurance · Gym membership.

## Features

- Monthly recurring spend (total + per-merchant).
- Subscription overview.
- Price-increase detection.

## Maps to the stack (light)

- **Entities:** `RecurringSeries` (merchant, cadence, expected amount, last/next seen, status).
- **API (`/api/v1`):**
  - `GET /recurring` (detected + confirmed series, with monthly total).
  - `POST /recurring/{id}/confirm` · `POST /recurring/{id}/ignore`.
  - `GET /recurring/{id}/history` (amount over time → price-increase view).
- **Detection:** a background job clusters transactions by normalized merchant + cadence (amount
  similarity + regular interval); surfaces candidates for one-tap confirmation. Amount drift across
  occurrences flags a price increase.
- **UI:** subscriptions list with monthly cost, confirm/ignore actions, and a price-history sparkline.
- **Dependencies:** consumes `Transaction` data (incl. imports from
  [Budgeting (1)](./01-spending-and-budgeting.md)); complements
  [Rules Engine (8)](./08-rules-engine.md); feeds [Cash Flow (6)](./06-cash-flow-forecasting.md) and
  [Insights (9)](./09-insights-and-analytics.md).

## Open questions / future

- Detection approach: heuristics first vs. ML later; minimum occurrences before suggesting a series.
- Merchant normalization (raw bank descriptors are noisy) — shared with the Rules Engine.
- Variable-amount recurrences (utilities) vs. fixed (Netflix) — different cadence/amount tolerances.
