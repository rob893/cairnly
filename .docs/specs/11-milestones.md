# Pillar 11 — Milestones

> Inspired by physical trail cairns. Major milestones become markers on the financial journey.

← [Back to overview](./README.md)

## Summary

The emotional core of Cairnly. Long-term finance is a slow trail; milestones are the **cairns** that
prove progress and sustain motivation. This pillar watches the other pillars and celebrates the
moments that matter.

## User value / outcomes

- Tangible, motivating proof of progress on a multi-year journey.
- A sense of momentum that keeps users engaged through slow stretches.
- Shareable, meaningful moments tied to real financial events.

## Examples

- 🪨 Emergency fund complete
- 🪨 Debt free
- 🪨 $100k net worth
- 🪨 $500k net worth
- 🪨 First million
- 🪨 Mortgage paid off
- 🪨 Coast FI
- 🪨 Financial Independence

## Features

- A curated set of milestone types (net-worth thresholds, debt-free, goal completions, FI markers).
- Achieved vs. upcoming state, with progress toward the next marker.
- Celebration moment when a milestone is reached.
- A "journey" view — the trail of cairns already placed.

## Maps to the stack (light)

- **Entities:** `Milestone` (`type`, `target`, `status`, `achievedAt`).
- **API (`/api/v1`):**
  - `GET /milestones` (achieved + upcoming, with progress to next).
  - `POST /milestones/{id}/acknowledge` (dismiss the celebration).
- **Detection:** milestone checks run when source data changes — net-worth snapshots
  ([Pillar 2](./02-net-worth-tracking.md)), goal completion ([Pillar 5](./05-goals.md)), debt
  reaching zero, and FI thresholds ([Pillar 10](./10-financial-independence-planning.md)) — and flip
  status to achieved.
- **UI:** a "journey"/trail visualization of cairns, the next-marker progress, and a celebratory
  moment (the cairn metaphor made visible — see the [overview](./README.md#the-cairn-metaphor)).
- **Dependencies:** observes [Net Worth (2)](./02-net-worth-tracking.md),
  [Goals (5)](./05-goals.md), and [FI Planning (10)](./10-financial-independence-planning.md).

## Open questions / future

- Fixed catalog of milestones vs. user-defined custom cairns.
- Threshold currency/normalization for net-worth milestones (multi-currency users).
- Notification/celebration design and whether milestones are ever shareable externally.
