import { cadenceBreakdown } from '../../utils/cadence';
import { minorToMajor } from '../../utils/money';
import type { Category } from '../../types/categories';
import type { SpendingPlanExpense, SpendingPlanIncome } from '../../types/spendingPlans';

const PALETTE = ['var(--accent)', '#38bdf8', '#a78bfa', '#fbbf24', '#fb7185', '#34d399', '#f472b6', '#60a5fa'];
export const OTHER_COLOR = '#94a3b8';
const INCOME_COLOR = 'var(--success)';
const INCOME_SOURCE_COLOR = 'var(--success)';
const SAVINGS_COLOR = 'var(--success)';
const FLOW_PALETTE = [
  '#fb923c',
  '#f472b6',
  '#c084fc',
  '#fbbf24',
  '#f87171',
  '#60a5fa',
  '#a78bfa',
  '#22d3ee',
  '#facc15',
  '#fb7185',
  '#34d399',
  '#818cf8',
  '#e879f9',
  '#fdba74',
  '#a3e635',
  '#38bdf8'
];

/** How the cash-flow Sankey groups the expense side. */
export type FlowMode = 'category' | 'group' | 'category-group';

/** How a composition donut groups its line items. */
export type DonutMode = 'source' | 'group' | 'category';

/** A node in the cash-flow Sankey, carrying display metadata Recharts preserves. */
export interface FlowNode {
  name: string;
  color: string;
  icon?: string;
}

/** Cash-flow Sankey data plus total monthly income for node percentage labels. */
export interface FlowData {
  nodes: FlowNode[];
  links: { source: number; target: number; value: number }[];
  totalIncome: number;
}

/** Recharts node payload enriched with layout totals plus Cairnly node metadata. */
export type FlowNodePayload = FlowNode & { value: number; depth: number };

/** A single composition slice (monthly amount in minor units). */
export interface Slice {
  name: string;
  value: number;
  color: string;
}

interface Bucket {
  name: string;
  icon?: string;
  value: number;
}

function monthlyOf(item: SpendingPlanIncome | SpendingPlanExpense): number {
  return Math.round(cadenceBreakdown(item.amount, item.cadence).monthly);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null;
}

function toSlices(entries: ReadonlyArray<{ name: string; value: number }>): Slice[] {
  const sorted = entries.filter(entry => entry.value > 0).sort((a, b) => b.value - a.value);
  const topCount = 6;
  const top = sorted.slice(0, topCount);
  const rest = sorted.slice(topCount);
  const slices: Slice[] = top.map((entry, index) => ({ ...entry, color: PALETTE[index % PALETTE.length] }));

  if (rest.length > 0) {
    slices.push({
      name: `Other (${rest.length})`,
      value: rest.reduce((sum, entry) => sum + entry.value, 0),
      color: OTHER_COLOR
    });
  }

  return slices;
}

/** Builds a top-N monthly composition from line items, grouped by source, category, or parent group. */
export function buildComposition(
  items: ReadonlyArray<SpendingPlanIncome | SpendingPlanExpense>,
  categoriesById?: Map<number, Category>,
  mode: DonutMode = 'source'
): Slice[] {
  if (mode === 'source' || !categoriesById) {
    return toSlices(items.map(item => ({ name: item.name, value: monthlyOf(item) })));
  }

  const buckets = new Map<string, { name: string; value: number }>();
  for (const item of items) {
    const value = monthlyOf(item);
    if (value <= 0) {
      continue;
    }

    const category = categoriesById.get(item.categoryId);
    const target =
      mode === 'category' ? category : category?.parentId != null ? categoriesById.get(category.parentId) : category;
    const key = target ? `cat-${target.id}` : `item-${item.id}`;
    const label = target ? (target.icon ? `${target.icon} ${target.name}` : target.name) : item.name;
    const existing = buckets.get(key);

    if (existing) {
      existing.value += value;
    } else {
      buckets.set(key, { name: label, value });
    }
  }

  return toSlices([...buckets.values()]);
}

/** Builds the income-to-expenses monthly cash-flow graph for the Sankey chart. */
export function buildFlow(
  incomes: ReadonlyArray<SpendingPlanIncome>,
  expenses: ReadonlyArray<SpendingPlanExpense>,
  categoriesById: Map<number, Category>,
  mode: FlowMode
): FlowData | null {
  const incomeBuckets = new Map<string, Bucket>();
  for (const item of incomes) {
    const value = monthlyOf(item);
    if (value <= 0) {
      continue;
    }

    const category = categoriesById.get(item.categoryId);
    const key = category ? `cat-${category.id}` : `item-${item.id}`;
    const existing = incomeBuckets.get(key);

    if (existing) {
      existing.value += value;
    } else {
      incomeBuckets.set(key, { name: category?.name ?? item.name, icon: category?.icon ?? undefined, value });
    }
  }

  const totalIncome = [...incomeBuckets.values()].reduce((sum, bucket) => sum + bucket.value, 0);
  if (totalIncome <= 0) {
    return null;
  }

  interface Group extends Bucket {
    leaves: Map<string, Bucket>;
  }
  const groups = new Map<string, Group>();
  for (const item of expenses) {
    const value = monthlyOf(item);
    if (value <= 0) {
      continue;
    }

    const category = categoriesById.get(item.categoryId);
    const parent = category?.parentId != null ? categoriesById.get(category.parentId) : undefined;
    const groupKey = parent ? `grp-${parent.id}` : category ? `cat-${category.id}` : `item-${item.id}`;
    const groupName = parent?.name ?? category?.name ?? 'Other';
    const groupIcon = parent?.icon ?? (parent ? undefined : (category?.icon ?? undefined));
    const leafKey = category ? `cat-${category.id}` : `item-${item.id}`;
    let group = groups.get(groupKey);

    if (!group) {
      group = { name: groupName, icon: groupIcon ?? undefined, value: 0, leaves: new Map() };
      groups.set(groupKey, group);
    }
    group.value += value;

    const leaf = group.leaves.get(leafKey);
    if (leaf) {
      leaf.value += value;
    } else {
      group.leaves.set(leafKey, { name: category?.name ?? item.name, icon: category?.icon ?? undefined, value });
    }
  }

  const nodes: FlowNode[] = [];
  const links: FlowData['links'] = [];
  const addNode = (node: FlowNode): number => nodes.push(node) - 1;
  const incomeIdx = addNode({ name: 'Income', color: INCOME_COLOR });

  for (const bucket of [...incomeBuckets.values()].sort((a, b) => b.value - a.value)) {
    const idx = addNode({ name: bucket.name, color: INCOME_SOURCE_COLOR, icon: bucket.icon });
    links.push({ source: idx, target: incomeIdx, value: bucket.value });
  }

  const totalExpenses = [...groups.values()].reduce((sum, group) => sum + group.value, 0);
  const savings = totalIncome - totalExpenses;
  if (savings > 0) {
    const savingsIdx = addNode({ name: 'Savings', color: SAVINGS_COLOR, icon: '💰' });
    links.push({ source: incomeIdx, target: savingsIdx, value: savings });
  }

  const sortedGroups = [...groups.values()].sort((a, b) => b.value - a.value);
  sortedGroups.forEach((group, index) => {
    const color = FLOW_PALETTE[index % FLOW_PALETTE.length];
    const leaves = [...group.leaves.values()].sort((a, b) => b.value - a.value);

    if (mode === 'group') {
      const groupIdx = addNode({ name: group.name, color, icon: group.icon });
      links.push({ source: incomeIdx, target: groupIdx, value: group.value });
    } else if (mode === 'category') {
      for (const leaf of leaves) {
        const leafIdx = addNode({ name: leaf.name, color, icon: leaf.icon });
        links.push({ source: incomeIdx, target: leafIdx, value: leaf.value });
      }
    } else {
      const groupIdx = addNode({ name: group.name, color, icon: group.icon });
      links.push({ source: incomeIdx, target: groupIdx, value: group.value });
      for (const leaf of leaves) {
        const leafIdx = addNode({ name: leaf.name, color, icon: leaf.icon });
        links.push({ source: groupIdx, target: leafIdx, value: leaf.value });
      }
    }
  });

  return { nodes, links, totalIncome };
}

/** Formats a minor-unit amount as a compact whole-dollar axis label. */
export function compactMoney(minorUnits: number, currency: string): string {
  const major = minorToMajor(minorUnits, currency);
  const abs = Math.abs(major);
  const sign = major < 0 ? '-' : '';

  if (abs >= 1000) {
    return `${sign}$${Math.round(abs / 1000)}k`;
  }

  return `${sign}$${Math.round(abs)}`;
}

/** Checks whether an unknown Recharts payload contains Cairnly Sankey node metadata. */
export function isFlowNode(value: unknown): value is FlowNode {
  return (
    isRecord(value) &&
    typeof value.name === 'string' &&
    typeof value.color === 'string' &&
    (value.icon === undefined || typeof value.icon === 'string')
  );
}

/** Checks whether an unknown Recharts node payload has the geometry-derived values this UI renders. */
export function isFlowNodePayload(value: unknown): value is FlowNodePayload {
  return isFlowNode(value) && isRecord(value) && typeof value.value === 'number' && typeof value.depth === 'number';
}

/** Returns a typed Sankey link target from an unknown Recharts link payload. */
export function getFlowLinkTarget(payload: unknown): FlowNode | undefined {
  if (!isRecord(payload)) {
    return undefined;
  }

  return isFlowNode(payload.target) ? payload.target : undefined;
}
