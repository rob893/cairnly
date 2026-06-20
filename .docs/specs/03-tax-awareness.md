# Pillar 3 — Tax Awareness

> Estimate after-tax income and effective tax burden.

← [Back to overview](./README.md)

## Summary

Translate gross compensation into realistic take-home pay. Most budgeting tools ignore taxes;
Cairnly makes the gap between salary and spendable income explicit, so plans are grounded in
after-tax reality.

## User value / outcomes

- A believable monthly take-home number to budget against.
- Understanding of marginal vs. effective rates and where money goes before it arrives.
- Better decisions about pre-tax contributions (401(k), HSA) and RSU/bonus timing.

## Inputs

Salary · Bonuses · RSUs · 401(k) · HSA · Filing status · State.

## Features

- Federal tax estimates.
- State tax estimates.
- FICA calculations (Social Security + Medicare).
- Effective tax rate.
- Monthly take-home estimate.
- True after-tax spending analysis (combine with [Budgeting (1)](./01-spending-and-budgeting.md)).

## Example interactions

- "Your estimated effective federal+state rate is **22.4%**."
- "Estimated monthly take-home: **$6,420**."
- "Maxing your HSA would lower taxable income by **$4,300**."

## Maps to the stack (light)

- **Entities:** `TaxProfile` (inputs), plus a versioned **tax table** reference set (brackets,
  standard deductions, FICA thresholds) keyed by tax year.
- **API (`/api/v1`):**
  - `GET/PUT /tax-profile`.
  - `POST /tax/estimate` (profile → federal/state/FICA breakdown, effective rate, take-home).
- **Compute:** a pure, well-tested tax-estimation service (deterministic given inputs + tax year);
  isolate the rules so they're easy to update annually and unit-test heavily.
- **UI:** a tax inputs form and a "where your paycheck goes" breakdown (gross → pretax → taxes →
  take-home).
- **Dependencies:** take-home feeds [Dashboard (4)](./04-financial-dashboard.md) and
  [FI Planning (10)](./10-financial-independence-planning.md).

## Open questions / future

- Scope of state coverage for MVP (all 50 states is a large rules surface — start with a subset?).
- Estimation fidelity: simple bracket math vs. credits/deductions/phaseouts — explicitly "estimate".
- Tax-year data maintenance: where the tables live and how they're updated each year.
- Strong, persistent disclaimer that this is **not tax advice**.
