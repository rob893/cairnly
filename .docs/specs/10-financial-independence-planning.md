# Pillar 10 — Financial Independence Planning

> Project long-term outcomes.

← [Back to overview](./README.md)

## Summary

Zoom all the way out. Using current net worth, cash flow, and a set of assumptions, Cairnly projects
the path to financial independence — and lets users play out "what if" decisions before making them.

## User value / outcomes

- A concrete FI number and an estimated date to reach it.
- Understanding of how today's choices change tomorrow's timeline.
- Confidence to make big decisions (raise, move, retire early) with eyes open.

## Inputs

Current net worth · Income · Expenses · Expected returns · Inflation · Savings rate · Retirement age.

## Features

- FI number calculation (e.g., annual expenses ÷ withdrawal rate).
- Retirement projections.
- Years remaining.
- Withdrawal analysis (e.g., safe-withdrawal-rate sustainability).
- Scenario modeling.

### What-if simulations

- Pay off mortgage early.
- Increase savings rate.
- Receive a raise.
- Buy a new home.
- Change jobs.
- Retire earlier.

## Maps to the stack (light)

- **Entities:** `FiPlan` (assumptions); pulls live values from
  [Net Worth (2)](./02-net-worth-tracking.md) and cash flow.
- **API (`/api/v1`):**
  - `GET/PUT /fi-plan`.
  - `POST /fi/project` (assumptions → FI number, years remaining, balance trajectory).
  - `POST /fi/scenarios` (baseline + named what-ifs → comparable projections).
- **Compute:** a deterministic projection engine (compounding growth, inflation, contributions,
  withdrawals) — pure and heavily unit-tested; scenarios are parameter overrides on the baseline.
- **UI:** an assumptions panel, a long-horizon projection chart, and side-by-side scenario
  comparison.
- **Dependencies:** consumes [Net Worth (2)](./02-net-worth-tracking.md),
  [Budgeting (1)](./01-spending-and-budgeting.md) (expenses/savings rate), and
  [Tax (3)](./03-tax-awareness.md) (after-tax income); FI milestones flow to
  [Milestones (11)](./11-milestones.md).

## Open questions / future

- Deterministic projections vs. Monte Carlo (return-sequence risk) — start deterministic.
- How conservative the default assumptions should be, and how clearly to label uncertainty.
- Persistent **not financial advice** disclaimer.
