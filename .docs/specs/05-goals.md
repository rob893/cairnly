# Pillar 5 — Goals

> Track progress toward financial objectives.

← [Back to overview](./README.md)

## Summary

Give saving a purpose. Goals turn an abstract "save more" into concrete, trackable targets with
progress bars and projected completion dates — and feed the momentum that becomes a
[Milestone (11)](./11-milestones.md).

## User value / outcomes

- Clear, motivating targets with visible progress.
- Realistic completion estimates based on current funding pace.
- Recommendations on how much to set aside to hit a date.

## Examples

Emergency fund · Vacation · Home down payment · Mortgage payoff · Car purchase · Band equipment ·
Retirement.

## Features

- Target amount.
- Current progress.
- Expected completion date (projected from funding pace).
- Funding recommendations ("set aside $X/month to finish by <date>").
- Goal status indicators (on track / behind / complete).

## Maps to the stack (light)

- **Entities:** `Goal` (optionally `linkedAccountId` so progress can track a real balance).
- **API (`/api/v1`):**
  - `GET/POST/PUT/DELETE /goals`.
  - `POST /goals/{id}/contributions` (manual progress) and/or derive progress from a linked account.
  - `GET /goals/{id}/projection` (expected completion + recommended monthly funding).
- **Compute:** projection from recent contribution rate (or linked-account growth); recommendation
  inverts target/date into a monthly amount.
- **UI:** goal cards with progress rings, a create/edit form, and status chips.
- **Dependencies:** consumes funding capacity from [Cash Flow (6)](./06-cash-flow-forecasting.md);
  completion can trigger [Milestones (11)](./11-milestones.md).

## Open questions / future

- Linked-account vs. manual-contribution goals (or both) — reconciliation rules.
- Goal prioritization / ordering when surplus is limited.
- Whether goal funding competes with budget envelopes or is tracked separately.
