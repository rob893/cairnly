# Pillar 1 — Spending & Budgeting

> Understand where money is going and compare actual spending against plans.

← [Back to overview](./README.md)

## Summary

The foundation of Cairnly: capture every dollar of income and expense, organize it, and hold it up
against a plan. This pillar turns a pile of transactions into an answerable question — *"Am I on
track this month?"*

## User value / outcomes

- A clear, categorized picture of where money goes.
- Budgets that reflect how the user actually thinks (monthly bills, weekly groceries, annual gifts).
- Always-available "how much can I still spend?" answers.

## Features

- Manual income and expense entry.
- CSV import from banks and credit cards.
- Transaction categorization.
- User-defined categories and tags.
- Monthly, weekly, daily, and annual budgets.
- Budget vs. actual reporting.
- Envelope-style budgeting (allocate to category envelopes; optional rollover).
- Variable and recurring expense tracking.
- Remaining-spend calculations.
- Savings-rate calculations.

## Example insights / interactions

- "You're on pace to save **$18,000** this year."
- "Restaurant spending is **20% over budget**."
- "You have **$350 left** for discretionary spending this month."

## Maps to the stack (light)

- **Entities:** `Transaction`, `Category`, `Tag`, `Budget` (see [domain model](./README.md#cross-cutting-domain-model)).
- **API (`/api/v1`):**
  - `GET/POST/PUT/DELETE /transactions` (cursor-paginated list with filters: date range, category, tag, account).
  - `POST /transactions/import` (CSV upload → staged rows → confirm).
  - `GET/POST/PUT/DELETE /categories`, `GET/POST/DELETE /tags`.
  - `GET/POST/PUT/DELETE /budgets`; `GET /budgets/status?period=monthly` (budget vs. actual + remaining).
  - `GET /reports/savings-rate?period=…`.
- **UI:** transactions table (filter/sort, inline categorize), budget editor, "budget vs. actual"
  cards, an import wizard (map columns → preview → commit).
- **Patterns:** reuse `Repository<>`/`CursorPaginatedList<T>`; budget-status is a read aggregate.
- **Dependencies:** CSV import feeds [Rules Engine (8)](./08-rules-engine.md) and
  [Recurring Detection (7)](./07-recurring-transaction-detection.md); categories/tags are shared
  with [Insights (9)](./09-insights-and-analytics.md).

## Open questions / future

- CSV format normalization — per-institution mappers vs. a generic column-mapping UI?
- Split transactions: model now (shared with Rules Engine) or defer?
- Handling transfers between own accounts so they don't count as income/expense.
- Mid-period budget changes and how rollover interacts with envelope balances.
