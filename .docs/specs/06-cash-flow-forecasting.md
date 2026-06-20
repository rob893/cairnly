# Pillar 6 — Cash Flow Forecasting

> Visualize upcoming income and expenses.

← [Back to overview](./README.md)

## Summary

Look forward, not just back. By projecting known income and expenses onto a calendar, Cairnly warns
about the dip before it happens and shows how much surplus is genuinely available.

## User value / outcomes

- Confidence that the rent/mortgage will clear.
- Early warning of low-balance moments and potential shortfalls.
- A trustworthy "safe to spend / save" number.

## Calendar view

Upcoming, dated items: Paychecks · Mortgage · Utilities · Insurance · Subscriptions · Credit cards.

## Forecasting

- Lowest projected balance (and when it occurs).
- Expected monthly surplus.
- Upcoming large expenses.
- Potential cash shortages (alerts).

## Maps to the stack (light)

- **Entities:** reads `RecurringSeries` (from [Pillar 7](./07-recurring-transaction-detection.md)),
  `Account` balances, scheduled income; optional `ScheduledItem` for known one-offs.
- **API (`/api/v1`):**
  - `GET /forecast?from=…&to=…` (daily projected balance series + lowest-balance marker).
  - `GET /forecast/calendar?month=…` (dated inflows/outflows).
- **Compute:** seed with current balance, then apply recurring + scheduled items forward; flag any
  day the projected balance crosses a threshold.
- **UI:** a calendar of inflows/outflows and a projected-balance line chart with the trough
  highlighted; shortfall banners.
- **Dependencies:** depends on [Recurring Detection (7)](./07-recurring-transaction-detection.md);
  feeds [Dashboard (4)](./04-financial-dashboard.md) and [Goals (5)](./05-goals.md) funding capacity.

## Open questions / future

- Modeling variable amounts (e.g., utilities) — averages, ranges, or last-N estimates?
- Handling irregular income (freelance, bonuses) and pay-cycle alignment.
- Confidence bands on the projection vs. a single deterministic line.
