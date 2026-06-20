# Pillar 8 — Rules Engine

> Automatically organize imported transactions.

← [Back to overview](./README.md)

## Summary

Keep data tidy without manual grind. User-defined rules match incoming transactions and apply
categories, tags, and splits — so imports arrive already organized and reporting stays accurate.

## User value / outcomes

- Imports that categorize themselves.
- Consistent organization that makes [Insights (9)](./09-insights-and-analytics.md) trustworthy.
- Power-user control without spreadsheets.

## Examples

```
IF merchant contains "Costco"        THEN category = Groceries
IF merchant contains "Guitar Center" THEN tag = Hobby
IF amount > $500                     THEN tag = "Large Expense"
```

## Features

- Merchant matching (contains / equals / regex).
- Category assignment.
- Tags.
- Split transactions (one transaction → multiple categorized parts).
- Custom rules (composable conditions + actions, priority-ordered).

## Maps to the stack (light)

- **Entities:** `Rule` (`conditions[]`, `actions[]`, `priority`, `enabled`); `Transaction.isSplit` +
  child rows for splits.
- **API (`/api/v1`):**
  - `GET/POST/PUT/DELETE /rules`; `PUT /rules/order` (priority).
  - `POST /rules/preview` (dry-run a rule against existing transactions).
  - `POST /rules/apply` (re-run rules over a date range / un-reviewed set).
- **Evaluation:** rules run on import and on demand, in priority order; first/most-specific match
  wins per field, or accumulate actions — define precedence explicitly. Keep evaluation pure and
  unit-tested.
- **UI:** rule builder (condition rows + action rows), live preview of affected transactions, and a
  split editor.
- **Dependencies:** operates on `Transaction`/`Category`/`Tag`; shares merchant normalization with
  [Recurring Detection (7)](./07-recurring-transaction-detection.md); cleans data for
  [Insights (9)](./09-insights-and-analytics.md).

## Open questions / future

- Condition grammar scope (AND/OR groups, regex) for MVP vs. later.
- Conflict resolution when multiple rules target the same field.
- Whether applying a rule retroactively re-writes history or only future imports (with an explicit
  "apply to existing" action).
